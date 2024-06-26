using Exanite.Networking;
using Exanite.Networking.Channels;
using UniDi;

namespace Source.Networking.Internal
{
    public class ChanneledNetworkInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind(typeof(IChanneledNetwork), typeof(INetwork), typeof(IPacketHandler))
                .To<ChanneledNetwork>()
                .FromMethod(ctx =>
                {
                    return new ChanneledNetwork(PacketHandlerIds.ChanneledNetwork, ctx.Container.Resolve<Network>());
                })
                .AsSingle();
        }
    }
}
