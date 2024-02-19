using Cysharp.Threading.Tasks;
using Exanite.Networking;
using Exanite.Networking.Transports.LiteNetLib;
using UnityEngine;

namespace Source.Networking
{
    public class ServerManager : MonoBehaviour
    {
        public NetworkServer server;
        public LnlTransportServer transport;

        private void Start()
        {
            server.ConnectionStarted += (network, connection) =>
            {
                Debug.Log("Server connected");
            };

            server.ConnectionStopped += (network, connection) =>
            {
                Debug.Log("Server disconnected");
            };

            server.SetTransports(new[] { transport });
            server.StartConnection().Forget();
        }
    }
}
