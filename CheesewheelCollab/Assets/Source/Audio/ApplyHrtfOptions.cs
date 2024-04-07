using UnityEngine;

namespace Source.Audio
{
    public struct ApplyHrtfOptions
    {
        public Vector3 OffsetToSound;
        public AnimationCurve AttenuationCurve;
        public float AttenuationStart;
        public float AttenuationEnd;

        // All of these buffers can be modified by the Hrtf class
        public float[] PreviousChunk;
        public float[] CurrentChunk;
        public float[] NextChunk;
        public float[] LeftChannel;
        public float[] RightChannel;
        public float[] ResultsBuffer;
    }
}
