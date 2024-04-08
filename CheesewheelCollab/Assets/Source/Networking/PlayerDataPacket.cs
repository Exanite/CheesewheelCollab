using Exanite.Networking;
using LiteNetLib.Utils;

namespace Source.Networking
{
    public class PlayerDataPacket : INetworkSerializable
    {
        public int PlayerId;
        public string Name;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(PlayerId);
            writer.Put(Name);
        }

        public void Deserialize(NetDataReader reader)
        {
            PlayerId = reader.GetInt();
            Name = reader.GetString();
        }
    }
}
