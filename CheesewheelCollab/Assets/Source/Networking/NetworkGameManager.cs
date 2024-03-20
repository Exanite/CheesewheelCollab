using System;
using System.Collections.Generic;
using csmatio.io;
using Cysharp.Threading.Tasks;
using Exanite.Core.Numbers;
using Exanite.Core.Utilities;
using Exanite.Networking;
using Exanite.Networking.Channels;
using Exanite.SceneManagement;
using Source.Audio;
using UniDi;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Network = Exanite.Networking.Network;

namespace Source.Networking
{
    public class NetworkGameManager : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject localPlayerPrefab;
        [SerializeField] private SceneIdentifier mainMenuScene;
        [FormerlySerializedAs("audioRecorder")]
        [SerializeField] private AudioProvider audioProvider;

        [Header("Audio Processing")]
        [Range(0, 1)]
        [SerializeField] private float volume = 1;
        [SerializeField] private int minChunksBuffered = 5;
        [SerializeField] private int maxChunksBuffered = 10;
        [SerializeField] private int minChunksQueued = 2;
        [FormerlySerializedAs("hrtfSubject")]
        [SerializeField] private HrtfSubject selectedSubject = HrtfSubject.Subject058;

        [Inject] private IEnumerable<IPacketHandler> packetHandlers;
        [Inject] private Network coreNetwork;
        [Inject] private IChanneledNetwork network;

        private ClientData clientData;

        private Dictionary<int, Player> players = new();

        private INetworkChannel<PlayerJoinPacket> playerJoinPacketChannel;
        private INetworkChannel<PlayerLeavePacket> playerLeavePacketChannel;
        private INetworkChannel<PlayerUpdatePacket> playerUpdatePacketChannel;
        private INetworkChannel<AudioPacket> audioPacketChannel;

        private void Start()
        {
            foreach (var packetHandler in packetHandlers)
            {
                coreNetwork.RegisterPacketHandler(packetHandler);
            }

            playerJoinPacketChannel = network.CreateChannel<PlayerJoinPacket>(nameof(PlayerJoinPacket), SendType.Reliable, OnPlayerJoinPacket);
            playerLeavePacketChannel = network.CreateChannel<PlayerLeavePacket>(nameof(PlayerLeavePacket), SendType.Reliable, OnPlayerLeavePacket);
            playerUpdatePacketChannel = network.CreateChannel<PlayerUpdatePacket>(nameof(PlayerUpdatePacket), SendType.Unreliable, OnPlayerUpdatePacket);
            audioPacketChannel = network.CreateChannel<AudioPacket>(nameof(AudioPacket), SendType.Unreliable, OnAudioPacket);

            network.ConnectionStarted += OnConnectionStarted;
            network.ConnectionStopped += OnConnectionStopped;

            if (network.IsClient)
            {
                clientData = new ClientData();

                audioProvider.SamplesAvailable += (chunk, samples) =>
                {
                    audioPacketChannel.Message.Chunk = chunk;
                    samples.AsSpan().CopyTo(audioPacketChannel.Message.Samples);

                    audioPacketChannel.Write();
                    foreach (var connection in network.Connections)
                    {
                        audioPacketChannel.SendNoWrite(connection);
                    }
                };
            }

            coreNetwork.StartConnection().Forget(e =>
            {
                mainMenuScene.Load(LoadSceneMode.Single);
            });
        }

        private void OnDestroy()
        {
            if (network.IsClient)
            {
                clientData.Output.Dispose();
            }
        }

        private void FixedUpdate()
        {
            if (network.IsClient && clientData.LocalPlayer != null)
            {
                playerUpdatePacketChannel.Message.Position = clientData.LocalPlayer.GameObject.transform.position;
                playerUpdatePacketChannel.Write();
                foreach (var connection in network.Connections)
                {
                    playerUpdatePacketChannel.SendNoWrite(connection);
                }
            }
        }

        private void Update()
        {
            if (network.IsClient)
            {
                if (clientData.LoadedSubject != selectedSubject)
                {
                    var path = Application.streamingAssetsPath + $"/CIPIC/standard_hrir_database/{selectedSubject.ToFileName()}/hrir_final.matlab";
                    clientData.Hrtf = new Hrtf(new MatFileReader(path));

                    clientData.LoadedSubject = selectedSubject;
                }

                var processingBuffer = clientData.ProcessingBuffer;
                var outputBuffer = clientData.OutputBuffer;
                var output = clientData.Output;
                var queuedChunks = clientData.Output.QueuedSamplesPerChannel / AudioConstants.SamplesChunkSize;
                if (queuedChunks < minChunksQueued)
                {
                    foreach (var (_, player) in players)
                    {
                        if (player == clientData.LocalPlayer)
                        {
                            continue;
                        }

                        if (player.Audio.MaxReceivedChunk - player.Audio.LastOutputChunk > maxChunksBuffered)
                        {
                            player.Audio.LastOutputChunk = player.Audio.MaxReceivedChunk - maxChunksBuffered;
                        }

                        if (player.Audio.MaxReceivedChunk - player.Audio.LastOutputChunk > minChunksBuffered)
                        {
                            player.Audio.LastOutputChunk++;

                            ApplyHrtf(player);
                            for (var i = 0; i < processingBuffer.Length; i++)
                            {
                                outputBuffer[i] += processingBuffer[i];
                            }
                        }
                    }

                    // Don't modify code below when processing audio
                    for (var i = 0; i < processingBuffer.Length; i++)
                    {
                        processingBuffer[i] = Mathf.Clamp(processingBuffer[i], -1, 1);
                    }
                    output.QueueSamples(processingBuffer);
                    processingBuffer.AsSpan().Clear();
                }
            }
        }

        private void ApplyHrtf(Player player)
        {
            // --- Get variables and buffers ---
            var previousChunk = clientData.PreviousChunk;
            var currentChunk = clientData.CurrentChunk;
            var nextChunk = clientData.NextChunk;
            var leftChannel = clientData.LeftChannel;
            var rightChannel = clientData.RightChannel;
            var processingBuffer = clientData.ProcessingBuffer;
            var hrtf = clientData.Hrtf;

            // --- Calculate direction ---
            var directionToSound = player.GameObject.transform.position - clientData.LocalPlayer.GameObject.transform.position;
            directionToSound = directionToSound.Swizzle(Vector3Swizzle.XZY); // Need to swap Y and Z values

            var azimuth = clientData.Hrtf.GetAzimuth(directionToSound);
            var elevation = clientData.Hrtf.GetElevation(directionToSound);

            // --- Update audio buffers ---
            var buffers = player.Audio.Buffers;
            buffers[(player.Audio.LastOutputChunk - 2 + buffers.Length) % buffers.Length].AsSpan().CopyTo(previousChunk);
            buffers[(player.Audio.LastOutputChunk - 1 + buffers.Length) % buffers.Length].AsSpan().CopyTo(currentChunk);
            buffers[(player.Audio.LastOutputChunk - 0 + buffers.Length) % buffers.Length].AsSpan().CopyTo(nextChunk);

            // --- Apply ITD ---
            var delayInSamples = hrtf.GetItd(azimuth, elevation);

            currentChunk.AsSpan().CopyTo(leftChannel);
            currentChunk.AsSpan().CopyTo(rightChannel);

            // Add delay to start of left
            currentChunk.AsSpan().CopyTo(rightChannel);
            currentChunk.AsSpan().Slice(delayInSamples).CopyTo(leftChannel);
            nextChunk.AsSpan().Slice(0, delayInSamples).CopyTo(leftChannel.AsSpan().Slice(leftChannel.Length - delayInSamples - 1));

            // Swap buffers if needed
            if (hrtf.IsRight(azimuth))
            {
                var temp = leftChannel;
                leftChannel = rightChannel;
                rightChannel = temp;
            }

            // --- Apply HRTF ---

            var originalMaxAmplitude = 0f;
            for (var i = 0; i < AudioConstants.SamplesChunkSize; i++)
            {
                originalMaxAmplitude = Mathf.Max(originalMaxAmplitude, Mathf.Abs(currentChunk[i]));
            }

            var convolvedMaxAmplitude = 0f;

            var leftHrtf = hrtf.GetHrtf(azimuth, elevation, false);
            var rightHrtf = hrtf.GetHrtf(azimuth, elevation, true);

            hrtf.Convolve(previousChunk, currentChunk, nextChunk, leftHrtf).AsSpan().CopyTo(leftChannel);
            hrtf.Convolve(previousChunk, currentChunk, nextChunk, rightHrtf).AsSpan().CopyTo(rightChannel);

            for (var i = 0; i < AudioConstants.SamplesChunkSize; i++)
            {
                convolvedMaxAmplitude = Mathf.Max(convolvedMaxAmplitude, Mathf.Abs(leftChannel[i]), Mathf.Abs(rightChannel[i]));
            }

            // Reduce to original amplitude
            var amplitudeFactor = convolvedMaxAmplitude / originalMaxAmplitude;
            if (originalMaxAmplitude > 1)
            {
                // Reduce max amplitude to 1
                amplitudeFactor *= originalMaxAmplitude;
            }

            if (amplitudeFactor > 1)
            {
                for (var i = 0; i < AudioConstants.SamplesChunkSize; i++)
                {
                    leftChannel[i] /= amplitudeFactor;
                    rightChannel[i] /= amplitudeFactor;
                }
            }

            // --- Copy to output ---
            // Cannot change output size, otherwise we record and consume at different rates
            for (var i = 0; i < AudioConstants.SamplesChunkSize; i++)
            {
                // Zip left and right channels together and output
                processingBuffer[i * 2] = leftChannel[i];
                processingBuffer[i * 2 + 1] = rightChannel[i];
            }
        }

        private void OnConnectionStarted(INetwork _, NetworkConnection connection)
        {
            if (network.IsServer)
            {
                // Sync existing players to new player
                foreach (var (_, existingPlayer) in players)
                {
                    playerJoinPacketChannel.Message.PlayerId = existingPlayer.Id;
                    playerJoinPacketChannel.Message.IsLocal = false;
                    playerJoinPacketChannel.Send(connection);
                }

                var player = new Player
                {
                    Id = connection.Id,
                };
                players.Add(player.Id, player);

                // Notify all players of new player
                playerJoinPacketChannel.Message.PlayerId = player.Id;
                foreach (var networkConnection in network.Connections)
                {
                    playerJoinPacketChannel.Message.IsLocal = networkConnection.Id == player.Id;
                    playerJoinPacketChannel.Send(networkConnection);
                }
            }
        }

        private void OnConnectionStopped(INetwork _, NetworkConnection connection)
        {
            if (network.IsServer)
            {
                players.Remove(connection.Id);

                playerLeavePacketChannel.Message.PlayerId = connection.Id;
                playerLeavePacketChannel.Write();
                foreach (var networkConnection in network.Connections)
                {
                    playerLeavePacketChannel.SendNoWrite(networkConnection);
                }
            }

            if (network.IsClient)
            {
                mainMenuScene.Load(LoadSceneMode.Single);
            }
        }

        private void OnPlayerJoinPacket(NetworkConnection connection, PlayerJoinPacket message)
        {
            Debug.Log($"Player joined: {message.PlayerId}");

            var playerPrefabToInstantiate = playerPrefab;
            if (message.IsLocal)
            {
                playerPrefabToInstantiate = localPlayerPrefab;
            }

            var player = new Player
            {
                Id = message.PlayerId,
                GameObject = Instantiate(playerPrefabToInstantiate),
                Audio = new Player.PlayerAudioData(),
            };

            players.Add(player.Id, player);

            if (message.IsLocal)
            {
                clientData.LocalPlayer = player;
            }
        }

        private void OnPlayerLeavePacket(NetworkConnection connection, PlayerLeavePacket message)
        {
            Debug.Log($"Player left: {message.PlayerId}");

            players.Remove(message.PlayerId, out var removedPlayer);
            Destroy(removedPlayer.GameObject);
        }

        private void OnPlayerUpdatePacket(NetworkConnection connection, PlayerUpdatePacket message)
        {
            Debug.Log($"Player updated: {message.PlayerId} ({message.Position}) (IsServer: {network.IsServer})");

            if (network.IsServer)
            {
                message.PlayerId = connection.Id;
                playerUpdatePacketChannel.Write(message);
                foreach (var networkConnection in network.Connections)
                {
                    if (networkConnection.Id == message.PlayerId)
                    {
                        continue;
                    }

                    playerUpdatePacketChannel.SendNoWrite(networkConnection);
                }
            }

            if (network.IsClient)
            {
                if (message.PlayerId != clientData.LocalPlayer.Id && players.TryGetValue(message.PlayerId, out var player))
                {
                    player.Position = message.Position;
                    player.GameObject.transform.position = message.Position;
                }
            }
        }

        private void OnAudioPacket(NetworkConnection connection, AudioPacket message)
        {
            if (network.IsServer)
            {
                message.PlayerId = connection.Id;
                audioPacketChannel.Write(message);
                foreach (var other in network.Connections)
                {
                    if (connection == other)
                    {
                        continue;
                    }

                    audioPacketChannel.SendNoWrite(other);
                }
            }

            if (network.IsClient)
            {
                if (players.TryGetValue(message.PlayerId, out var player))
                {
                    player.Audio.MaxReceivedChunk = Mathf.Max(player.Audio.MaxReceivedChunk, message.Chunk);
                    message.Samples.AsSpan().CopyTo(player.Audio.Buffers[message.Chunk % player.Audio.Buffers.Length]);
                }
            }

            Debug.Log($"Received audio from {message.PlayerId} (IsServer: {network.IsServer})");
        }
    }
}
