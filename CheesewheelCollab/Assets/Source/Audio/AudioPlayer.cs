using UnityEngine;

namespace Source.Audio
{
    public class AudioPlayer : MonoBehaviour
    {
        [SerializeField] private AudioRecorder recorder;

        private float[][] buffers;

        private void Start()
        {
            buffers = new float[256][];
            for (var i = 0; i < buffers.Length; i++)
            {
                buffers[i] = new float[AudioConstants.AudioPacketSamplesSize];
            }

            recorder.SamplesRecorded += OnSamplesRecorded;
        }

        private void OnSamplesRecorded(int sequence, float[] samples)
        {
            samples.CopyTo(buffers[sequence % buffers.Length], 0);
        }
    }
}
