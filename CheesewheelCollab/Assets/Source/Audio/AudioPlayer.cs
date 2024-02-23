using Exanite.Core.Collections;
using UnityEngine;

namespace Source.Audio
{
    public class AudioPlayer : MonoBehaviour
    {
        [SerializeField] private AudioRecorder recorder;

        private RingBuffer<float[]> ringBuffer;

        private void Start()
        {
            ringBuffer = new RingBuffer<float[]>(256);
            for (var i = 0; i < ringBuffer.Count; i++)
            {
                ringBuffer[i] = new float[AudioConstants.AudioPacketSamplesSize];
            }
        }
    }
}
