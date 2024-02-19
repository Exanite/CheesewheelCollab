using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Exanite.Networking;
using Exanite.Networking.Channels;
using UniDi;
using UnityEngine;
using Network = Exanite.Networking.Network;

namespace Source.Networking
{
    public class NetworkManager : MonoBehaviour
    {
        [Inject] private IEnumerable<IPacketHandler> packetHandlers;
        [Inject] private Network coreNetwork;
        [Inject] private IChanneledNetwork network;

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

            coreNetwork.StartConnection().Forget();
        }
    }
}
