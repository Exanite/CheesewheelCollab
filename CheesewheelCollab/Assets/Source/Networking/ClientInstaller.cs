using Exanite.Networking;
using Exanite.Networking.Transports.LiteNetLib;
using UniDi;
using UnityEngine;
using Network = Exanite.Networking.Network;

namespace Source.Networking
{
    public class ClientInstaller : MonoInstaller
    {
        [SerializeField] private NetworkClient coreNetwork;
        [SerializeField] private LnlTransportClient transport;

        public override void InstallBindings()
        {
            Container.Bind(typeof(NetworkClient), typeof(Network))
                .To<NetworkClient>()
                .FromInstance(coreNetwork)
                .AsSingle()
                .OnInstantiated<NetworkClient>((_, _) =>
                {
                    coreNetwork.SetTransport(transport);
                })
                .NonLazy();
        }
    }
}
