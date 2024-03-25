using UnityEngine;

namespace Source.Audio
{
    public struct ApplyHrtfOptions
    {
        public Vector3 OffsetToSound;
            
        public float[] PreviousChunk;
        public float[] CurrentChunk;
        public float[] NextChunk;
        public float[] LeftChannel;
        public float[] RightChannel;
        public float[] ResultsBuffer;
    }
}