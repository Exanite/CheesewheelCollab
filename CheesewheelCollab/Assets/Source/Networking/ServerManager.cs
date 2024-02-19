using Exanite.Networking;
using Exanite.Networking.Transports.LiteNetLib;
using UnityEngine;

namespace Source.Networking
{
    public class ServerManager : MonoBehaviour
    {
        public NetworkServer server;
        public LnlTransportServer transport;

        private void Awake()
        {
            server.SetTransports(new[] { transport });
        }
    }
}
