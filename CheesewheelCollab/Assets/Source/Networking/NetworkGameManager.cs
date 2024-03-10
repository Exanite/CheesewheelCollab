using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Exanite.Networking;
using Exanite.Networking.Channels;
using Exanite.SceneManagement;
using Source.Audio;
using UniDi;
using UnityEngine;
using UnityEngine.SceneManagement;
using Network = Exanite.Networking.Network;

namespace Source.Networking
{
    public class NetworkGameManager : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject localPlayerPrefab;
        [SerializeField] private SceneIdentifier mainMenuScene;
        [SerializeField] private AudioRecorder audioRecorder;

        [Header("Audio")]
        [SerializeField] private int minChunksBuffered = 5;
        [SerializeField] private int maxChunksBuffered = 10;
        [SerializeField] private int minChunksQueued = 2;

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

                audioRecorder.SamplesAvailable += (chunk, samples) =>
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
                mainMenuScene.Load(LoadSceneMode.Additive);
            });
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
                mainMenuScene.Load(LoadSceneMode.Additive);
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
                    // // Todo Uncomment this
                    // if (connection == other)
                    // {
                    //     continue;
                    // }

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
