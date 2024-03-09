using Exanite.Networking;
using LiteNetLib.Utils;
using UnityEngine;

namespace Source.Networking
{
    public class PlayerUpdatePacket : INetworkSerializable
    {
        public int PlayerId;
        public Vector2 Position;

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(PlayerId);
            writer.Put(Position);
        }

        public void Deserialize(NetDataReader reader)
        {
            PlayerId = reader.GetInt();
            Position = reader.GetVector2();
        }
    }
}
