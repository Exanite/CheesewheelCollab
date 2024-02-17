using System.Collections.Generic;

namespace Exanite.Networking.Channels
{
    public interface IChanneledNetwork : INetwork
    {
        public IReadOnlyDictionary<string, INetworkChannel> Channels { get; }

        public INetworkChannel<T> CreateChannel<T>(string key, SendType sendType = SendType.Reliable) where T : INetworkSerializable, new()
        {
            return CreateChannel(key, new T(), sendType);
        }

        public INetworkChannel<T> CreateChannel<T>(string key, T packet, SendType sendType = SendType.Reliable) where T : INetworkSerializable;
    }
}
