using Exanite.Networking;
using LiteNetLib.Utils;

namespace Source.Networking
{
    public class PlayerLeavePacket : INetworkSerializable
    {
        public int PlayerId;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(PlayerId);
        }

        public void Deserialize(NetDataReader reader)
        {
            PlayerId = reader.GetInt();
        }
    }
}
