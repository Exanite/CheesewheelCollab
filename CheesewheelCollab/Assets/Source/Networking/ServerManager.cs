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
            server.SetTransports(new[] { transport });
        }
    }
}
