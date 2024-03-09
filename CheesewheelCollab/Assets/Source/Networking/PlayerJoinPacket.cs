using Exanite.Networking;
using LiteNetLib.Utils;

namespace Source.Networking
{
    public class PlayerJoinPacket : INetworkSerializable
    {
        public int PlayerId;
        public bool IsLocal;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(PlayerId);
            writer.Put(IsLocal);
        }

        public void Deserialize(NetDataReader reader)
        {
            PlayerId = reader.GetInt();
            IsLocal = reader.GetBool();
        }
    }
}
