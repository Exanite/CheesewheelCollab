using Exanite.Networking;
using Exanite.Networking.Transports.LiteNetLib;
using UniDi;
using UnityEngine;
using Network = Exanite.Networking.Network;

namespace Source.Networking
{
    public class ServerInstaller : MonoInstaller
    {
        [SerializeField] private NetworkServer coreNetwork;
        [SerializeField] private LnlTransportServer transport;

        public override void InstallBindings()
        {
            Container.Bind(typeof(NetworkServer), typeof(Network))
                .FromInstance(coreNetwork)
                .AsSingle()
                .OnInstantiated((_, _) =>
                {
                    coreNetwork.SetTransports(new[] { transport });
                })
                .NonLazy();
        }
    }
}
