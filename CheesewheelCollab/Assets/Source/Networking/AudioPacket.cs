using Exanite.Core.Utilities;
using Exanite.Networking;
using LiteNetLib.Utils;
using Source.Audio;

namespace Source.Networking
{
    public class AudioPacket : INetworkSerializable
    {
        /// <summary>
        /// Time the packet was recorded on the client. Probably should use ticks or packet counter instead.
        /// </summary>
        public float Time;

        // We'll probably send 500 samples with 2 bytes each (16 bit precision)
        // Max UDP MTU is 1460-ish, but we'll send ~1000 to be safe
        public readonly float[] Samples = new float[AudioConstants.SamplesChunkSize];

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(Time);
            for (var i = 0; i < Samples.Length; i++)
            {
                writer.Put((short)MathUtility.Remap(Samples[i], -1, 1, short.MinValue, short.MaxValue));
            }
        }

        public void Deserialize(NetDataReader reader)
        {
            Time = reader.GetFloat();
            for (var i = 0; i < Samples.Length; i++)
            {
                Samples[i] = (short)MathUtility.Remap(reader.GetShort(), short.MinValue, short.MaxValue, -1, 1);
            }
        }
    }
}
