using System;
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
        public float[] Data = new float[250];

        public void Serialize(NetDataWriter writer)
        {
            for (var i = 0; i < Data.Length; i++)
            {
                writer.Put(Data[i]);
            }
        }

        public void Deserialize(NetDataReader reader)
        {
            for (var i = 0; i < Data.Length; i++)
            {
                Data[i] = reader.GetFloat();
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
                for (var i = 0; i < audioPacketChannel.Message.Data.Length; i++)
                {
                    audioPacketChannel.Message.Data[i] = Time.time;
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
            Debug.Log($"Received on {connection.Network.GetType().Name}: {message.Data[0]}");
        }
    }
}
