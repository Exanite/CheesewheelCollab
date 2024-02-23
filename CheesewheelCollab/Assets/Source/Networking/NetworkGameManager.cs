using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Exanite.Networking;
using Exanite.Networking.Channels;
using LiteNetLib.Utils;
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

        [Inject] private IEnumerable<IPacketHandler> packetHandlers;
        [Inject] private Network coreNetwork;
        [Inject] private IChanneledNetwork network;

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
            playerUpdatePacketChannel = network.CreateChannel<PlayerUpdatePacket>(nameof(PlayerUpdatePacket), SendType.Reliable, OnPlayerUpdatePacket);
            audioPacketChannel = network.CreateChannel<AudioPacket>(nameof(AudioPacket), SendType.Unreliable, OnAudioPacket);

            network.ConnectionStarted += (_, _) =>
            {
                Debug.Log($"{(network.IsServer ? "Server" : "Client")} connected");
            };

            network.ConnectionStopped += (_, _) =>
            {
                Debug.Log($"{(network.IsServer ? "Server" : "Client")} disconnected");
            };

            network.ConnectionStarted += OnConnectionStarted;

            network.ConnectionStopped += OnConnectionStopped;

            coreNetwork.StartConnection().Forget();
        }

        private void FixedUpdate()
        {
            if (network.IsClient)
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
            }
        }

        private void OnConnectionStarted(INetwork _, NetworkConnection connection)
        {
            if (network.IsServer)
            {
                var player = new Player
                {
                    Id = connection.Id,
                };
                players.Add(player.Id, player);

                playerJoinPacketChannel.Message.PlayerId = player.Id;
                playerJoinPacketChannel.Write();
                foreach (var networkConnection in network.Connections)
                {
                    playerJoinPacketChannel.SendNoWrite(networkConnection);
                }
            }
        }

        private void OnConnectionStopped(INetwork network1, NetworkConnection connection)
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
        }

        private void OnPlayerJoinPacket(NetworkConnection connection, PlayerJoinPacket message)
        {
            Debug.Log($"Player joined: {message.PlayerId}");
        }

        private void OnPlayerLeavePacket(NetworkConnection connection, PlayerLeavePacket message)
        {
            Debug.Log($"Player left: {message.PlayerId}");
        }

        private void OnPlayerUpdatePacket(NetworkConnection connection, PlayerUpdatePacket message)
        {
            Debug.Log($"Player updated: {message.PlayerId} ({message.Position})");
        }

        private void OnAudioPacket(NetworkConnection connection, AudioPacket message)
        {
            Debug.Log($"Received on {connection.Network.GetType().Name}: {message.Time}");
        }
    }

    public class AudioPacket : INetworkSerializable
    {
        public const int SamplesLength = 250;

        /// <summary>
        /// Time the packet was recorded on the client. Probably should use ticks or packet counter instead.
        /// </summary>
        public float Time;

        // We'll probably send 500 samples with 2 bytes each (16 bit precision)
        // Max UDP MTU is 1460-ish, but we'll send ~1000 to be safe
        public readonly float[] Samples = new float[SamplesLength];

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Time);
            for (var i = 0; i < Samples.Length; i++)
            {
                writer.Put(Samples[i]);
            }
        }

        public void Deserialize(NetDataReader reader)
        {
            Time = reader.GetFloat();
            for (var i = 0; i < Samples.Length; i++)
            {
                Samples[i] = reader.GetFloat();
            }
        }
    }

    public class Player
    {
        public int Id;

        /// <summary>
        /// Null on server.
        /// </summary>
        public GameObject GameObject;
    }

    public class PlayerJoinPacket : INetworkSerializable
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
