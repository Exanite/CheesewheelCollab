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

    public class NetworkManager : MonoBehaviour
    {
        [Inject] private IEnumerable<IPacketHandler> packetHandlers;
        [Inject] private Network coreNetwork;
        [Inject] private IChanneledNetwork network;

        private INetworkChannel<AudioPacket> audioPacketChannel;

        private void Start()
        {
            foreach (var packetHandler in packetHandlers)
            {
                coreNetwork.RegisterPacketHandler(packetHandler);
            }

            network.ConnectionStarted += (_, _) =>
            {
                Debug.Log($"{(network.IsServer ? "Server" : "Client")} connected");
            };

            network.ConnectionStopped += (_, _) =>
            {
                Debug.Log($"{(network.IsServer ? "Server" : "Client")} disconnected");
            };

            audioPacketChannel = network.CreateChannel<AudioPacket>("AudioPacket", SendType.Unreliable, OnAudioPacket);

            coreNetwork.StartConnection().Forget();
        }

        private void Update()
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
                    audioPacketChannel.Send(connection);
                }
            }
        }

        private void OnAudioPacket(NetworkConnection connection, AudioPacket message)
        {
            Debug.Log($"Received on {connection.Network.GetType().Name}: {message.Time}");
        }
    }
}
