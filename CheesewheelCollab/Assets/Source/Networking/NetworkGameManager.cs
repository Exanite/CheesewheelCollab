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
using Source.Players;
using Source.UserInterface;
using UniDi;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Network = Exanite.Networking.Network;

namespace Source.Networking
{
    public class NetworkGameManager : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private PlayerCharacter playerPrefab;
        [SerializeField] private PlayerCharacter localPlayerPrefab;
        [SerializeField] private SceneIdentifier mainMenuScene;
        [FormerlySerializedAs("audioRecorder")]
        [SerializeField] private AudioProvider audioProvider;
        [SerializeField] private VolumeControlDisplay localPlayerVolumeControl;
        [SerializeField] private VolumeControlDisplay volumeControlPrefab;
        [SerializeField] private RectTransform volumeControlParent;
        [SerializeField] private Toggle hearSelfToggle;

        [Header("Audio Processing")]
        [Range(0, 1)]
        [SerializeField] private float volume = 1;
        [SerializeField] private int minChunksBuffered = 5;
        [SerializeField] private int maxChunksBuffered = 10;
        [SerializeField] private int minChunksQueued = 2;
        [FormerlySerializedAs("hrtfSubject")]
        [SerializeField] private HrtfSubject selectedSubject = HrtfSubject.Subject058;

        [Header("Audio Attenuation")]
        [SerializeField] private AnimationCurve attenuationCurve;
        [SerializeField] private float attenuationStart;
        [SerializeField] private float attenuationEnd;

        [Inject] private IInstantiator instantiator;
        [Inject] private IEnumerable<IPacketHandler> packetHandlers;
        [Inject] private Network coreNetwork;
        [Inject] private IChanneledNetwork network;
        [Inject] private LocalPlayerSettings localPlayerSettings;

        public ClientData ClientData { get; private set; }

        // Connection ID is the same as Player ID
        private Dictionary<int, Player> players = new();
        private Dictionary<int, VolumeControlDisplay> playerVolumeControls = new();
        private List<int> playerVolumeControlsToRemove = new();

        private INetworkChannel<PlayerJoinPacket> playerJoinPacketChannel;
        private INetworkChannel<PlayerLeavePacket> playerLeavePacketChannel;
        private INetworkChannel<PlayerDataPacket> playerDataPacketChannel;
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
            playerDataPacketChannel = network.CreateChannel<PlayerDataPacket>(nameof(PlayerDataPacket), SendType.Reliable, OnPlayerDataPacket);
            playerUpdatePacketChannel = network.CreateChannel<PlayerUpdatePacket>(nameof(PlayerUpdatePacket), SendType.Unreliable, OnPlayerUpdatePacket);
            audioPacketChannel = network.CreateChannel<AudioPacket>(nameof(AudioPacket), SendType.Unreliable, OnAudioPacket);

            network.ConnectionStarted += OnConnectionStarted;
            network.ConnectionStopped += OnConnectionStopped;

            if (network.IsClient)
            {
                ClientData = new ClientData();

                audioProvider.SamplesAvailable += (chunk, samples) =>
                {
                    if (ClientData.LocalPlayer == null)
                    {
                        return;
                    }

                    audioPacketChannel.Message.Chunk = chunk;
                    for (var i = 0; i < samples.Length; i++)
                    {
                        audioPacketChannel.Message.Samples[i] = samples[i] * ClientData.LocalPlayer.Volume;
                    }

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
                ClientData.Output.Dispose();
            }
        }

        private void FixedUpdate()
        {
            if (network.IsClient && ClientData.LocalPlayer != null)
            {
                playerUpdatePacketChannel.Message.Position = ClientData.LocalPlayer.Character.transform.position;
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
                // Update UI
                foreach (var (id, player) in players)
                {
                    if (!playerVolumeControls.ContainsKey(id) && player != ClientData.LocalPlayer)
                    {
                        var display = instantiator.InstantiatePrefabForComponent<VolumeControlDisplay>(volumeControlPrefab, volumeControlParent);
                        display.Player = player;

                        playerVolumeControls.Add(id, display);
                    }
                }

                foreach (var id in playerVolumeControls.Keys)
                {
                    if (!players.ContainsKey(id))
                    {
                        playerVolumeControlsToRemove.Add(id);
                    }
                }

                foreach (var id in playerVolumeControlsToRemove)
                {
                    Destroy(playerVolumeControls[id].gameObject);
                    playerVolumeControls.Remove(id);
                }
                playerVolumeControlsToRemove.Clear();

                // Load HRTF
                if (ClientData.LoadedSubject != selectedSubject)
                {
                    var path = Application.streamingAssetsPath + $"/CIPIC/standard_hrir_database/{selectedSubject.ToFileName()}/hrir_final.matlab";
                    ClientData.Hrtf = new Hrtf(new MatFileReader(path));

                    ClientData.LoadedSubject = selectedSubject;
                }

                // Process audio
                var output = ClientData.Output;
                var outputBuffer = ClientData.OutputBuffer;
                var queuedChunks = ClientData.Output.QueuedSamplesPerChannel / AudioConstants.SamplesChunkSize;
                if (queuedChunks < minChunksQueued)
                {
                    foreach (var (_, player) in players)
                    {
                        var isLocal = player == ClientData.LocalPlayer;
                        if (!hearSelfToggle.isOn && isLocal)
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

                            var results = ApplyHrtf(player);
                            for (var i = 0; i < results.Length; i++)
                            {
                                var volume = isLocal ? 1 : player.Volume;
                                outputBuffer[i] += results[i] * volume;
                            }
                        }
                    }

                    // Don't modify code below when processing audio
                    for (var i = 0; i < outputBuffer.Length; i++)
                    {
                        outputBuffer[i] *= volume;
                        outputBuffer[i] = Mathf.Clamp(outputBuffer[i], -1, 1);
                    }
                    output.QueueSamples(outputBuffer);
                    outputBuffer.AsSpan().Clear();
                }
            }
        }

        private float[] ApplyHrtf(Player player)
        {
            var offsetToSound = player.Character.transform.position - ClientData.LocalPlayer.Character.transform.position;
            offsetToSound = offsetToSound.Swizzle(Vector3Swizzle.XZY); // Need to swap Y and Z values

            if (player == ClientData.LocalPlayer)
            {
                offsetToSound = Vector3.forward * 0.1f;
            }

            var applyOptions = new ApplyHrtfOptions
            {
                OffsetToSound = offsetToSound,
                AttenuationCurve = attenuationCurve,
                AttenuationStart = attenuationStart,
                AttenuationEnd = attenuationEnd,

                PreviousChunk = ClientData.PreviousChunk,
                CurrentChunk = ClientData.CurrentChunk,
                NextChunk = ClientData.NextChunk,
                LeftChannel = ClientData.LeftChannel,
                RightChannel = ClientData.RightChannel,
                ResultsBuffer = ClientData.ResultsBuffer,
            };

            // --- Update audio buffers ---
            var buffers = player.Audio.Buffers;
            buffers[(player.Audio.LastOutputChunk - 2 + buffers.Length) % buffers.Length].AsSpan().CopyTo(applyOptions.PreviousChunk);
            buffers[(player.Audio.LastOutputChunk - 1 + buffers.Length) % buffers.Length].AsSpan().CopyTo(applyOptions.CurrentChunk);
            buffers[(player.Audio.LastOutputChunk - 0 + buffers.Length) % buffers.Length].AsSpan().CopyTo(applyOptions.NextChunk);

            return ClientData.Hrtf.Apply(applyOptions);
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

        private void SyncPlayerData()
        {
            if (network.IsClient)
            {
                playerDataPacketChannel.Message.Name = localPlayerSettings.PlayerName;
                foreach (var connection in network.Connections)
                {
                    playerDataPacketChannel.Send(connection);
                }
            }

            if (network.IsServer)
            {
                foreach (var player in players.Values)
                {
                    playerDataPacketChannel.Message.PlayerId = player.Id;
                    playerDataPacketChannel.Message.Name = player.Name;
                    foreach (var connection in network.Connections)
                    {
                        playerDataPacketChannel.Send(connection);
                    }
                }
            }
        }

        private void OnPlayerDataPacket(NetworkConnection connection, PlayerDataPacket message)
        {
            if (network.IsClient)
            {
                if (players.TryGetValue(message.PlayerId, out var player))
                {
                    player.Name = message.Name;
                }
            }

            if (network.IsServer)
            {
                if (players.TryGetValue(connection.Id, out var player))
                {
                    player.Name = message.Name;
                    SyncPlayerData();
                }
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
                Character = Instantiate(playerPrefabToInstantiate),
                Audio = new Player.PlayerAudioData(),
            };

            player.Character.Player = player;

            players.Add(player.Id, player);

            if (message.IsLocal)
            {
                ClientData.LocalPlayer = player;
                localPlayerVolumeControl.Player = player;
                SyncPlayerData();
            }
        }

        private void OnPlayerLeavePacket(NetworkConnection connection, PlayerLeavePacket message)
        {
            Debug.Log($"Player left: {message.PlayerId}");

            players.Remove(message.PlayerId, out var removedPlayer);
            Destroy(removedPlayer.Character.gameObject);
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
                if (message.PlayerId != ClientData.LocalPlayer.Id && players.TryGetValue(message.PlayerId, out var player))
                {
                    player.Position = message.Position;
                    player.Character.transform.position = message.Position;
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
