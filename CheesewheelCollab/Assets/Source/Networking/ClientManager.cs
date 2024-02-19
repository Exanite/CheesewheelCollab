using Exanite.Networking;
using Exanite.Networking.Transports.LiteNetLib;
using UnityEngine;

namespace Source.Networking
{
    public class ClientManager : MonoBehaviour
    {
        public NetworkClient client;
        public LnlTransportClient transport;

        private void Awake()
        {
            client.SetTransport(transport);
        }
    }
}
