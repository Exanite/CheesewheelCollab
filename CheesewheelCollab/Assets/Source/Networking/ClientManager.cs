using Cysharp.Threading.Tasks;
using Exanite.Networking;
using Exanite.Networking.Transports.LiteNetLib;
using UnityEngine;

namespace Source.Networking
{
    public class ClientManager : MonoBehaviour
    {
        public NetworkClient client;
        public LnlTransportClient transport;

        private void Start()
        {
            client.ConnectionStarted += (network, connection) =>
            {
                Debug.Log("Client connected");
            };

            client.ConnectionStopped += (network, connection) =>
            {
                Debug.Log("Client disconnected");
            };

            client.SetTransport(transport);
            client.StartConnection().Forget();
        }
    }
}
