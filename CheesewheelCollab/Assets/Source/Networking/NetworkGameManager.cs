using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Exanite.Core.Utilities;
using Exanite.Networking;
using Exanite.Networking.Channels;
using Exanite.SceneManagement;
using LiteNetLib.Utils;
using Source.Audio;
using UniDi;
using UnityEngine;
using Network = Exanite.Networking.Network;

namespace Source.Networking
{
    public class NetworkGameManager : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject localPlayerPrefab;
        [SerializeField] private SceneIdentifier mainMenuScene;

        [Inject] private IEnumerable<IPacketHandler> packetHandlers;
        [Inject] private Network coreNetwork;
        [Inject] private IChanneledNetwork network;

        [Inject] private SceneLoader sceneLoader;
        [Inject] private DiContainer container;
        [Inject] private SceneLoadManager sceneLoadManager;

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
            }

            coreNetwork.StartConnection().Forget();
        }

        private void FixedUpdate()
        {
            if (network.IsClient && clientData.LocalPlayer != null)
            {
                for (var i = 0; i < audioPacketChannel.Message.Samples.Length; i++)
                {
                    audioPacketChannel.Message.Time = Time.time;
                }

                audioPacketChannel.Write();
                foreach (var connection in network.Connections)
                {
                    audioPacketChannel.SendNoWrite(connection);
                }

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
                mainMenuScene.Load(sceneLoader, container, false);
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
            Debug.Log($"Received on {connection.Network.GetType().Name}: {message.Time} | Time diff: {Time.time - message.Time}");
        }
    }

    public class AudioPacket : INetworkSerializable
    {
        /// <summary>
        /// Time the packet was recorded on the client. Probably should use ticks or packet counter instead.
        /// </summary>
        public float Time;

        // We'll probably send 500 samples with 2 bytes each (16 bit precision)
        // Max UDP MTU is 1460-ish, but we'll send ~1000 to be safe
        public readonly float[] Samples = new float[AudioConstants.SamplesChunkSize];

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Time);
            for (var i = 0; i < Samples.Length; i++)
            {
                writer.Put((short)MathUtility.Remap(Samples[i], -1, 1, short.MinValue, short.MaxValue));
            }
        }

        public void Deserialize(NetDataReader reader)
        {
            Time = reader.GetFloat();
            for (var i = 0; i < Samples.Length; i++)
            {
                Samples[i] = (short)MathUtility.Remap(reader.GetShort(), short.MinValue, short.MaxValue, -1, 1);
            }
        }
    }

    public class Player
    {
        public int Id;

        public Vector2 Position;

        /// <summary>
        /// Null on server.
        /// </summary>
        public GameObject GameObject;
    }

    public class ClientData
    {
        public Player LocalPlayer;
    }

    public class PlayerJoinPacket : INetworkSerializable
    {
        public int PlayerId;
        public bool IsLocal;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(PlayerId);
            writer.Put(IsLocal);
        }

        public void Deserialize(NetDataReader reader)
        {
            PlayerId = reader.GetInt();
            IsLocal = reader.GetBool();
        }
    }

    public class PlayerLeavePacket : INetworkSerializable
    {
        public int PlayerId;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(PlayerId);
        }

        public void Deserialize(NetDataReader reader)
        {
            PlayerId = reader.GetInt();
        }
    }

    public class PlayerUpdatePacket : INetworkSerializable
    {
        public int PlayerId;
        public Vector2 Position;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(PlayerId);
            writer.Put(Position);
        }

        public void Deserialize(NetDataReader reader)
        {
            PlayerId = reader.GetInt();
            Position = reader.GetVector2();
        }
    }
}
