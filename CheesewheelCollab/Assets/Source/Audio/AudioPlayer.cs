using UnityEngine;

namespace Source.Audio
{
    public class AudioPlayer : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private AudioRecorder recorder;
        [SerializeField] private AudioSource audioSource;

        [Header("Settings")]
        private float delaySeconds = 1;

        private float[][] buffers;
        private AudioClip playbackClip;

        private void Start()
        {
            playbackClip = AudioClip.Create("Playback", AudioConstants.RecordingSampleRate * 10, 2, AudioConstants.RecordingSampleRate, false);
            audioSource.clip = playbackClip;
            audioSource.loop = true;
            audioSource.Play();

            var samples = new float[AudioConstants.AudioPacketSamplesSize];
            for (var chunkI = 0; chunkI < playbackClip.samples / AudioConstants.AudioPacketSamplesSize; chunkI++)
            {
                for (var sampleI = 0; sampleI < AudioConstants.AudioPacketSamplesSize; sampleI++)
                {
                    var time = (chunkI * sampleI) / AudioConstants.RecordingSampleRate;
                    var y = Mathf.Sin(2 * Mathf.PI * time * 440);
                    samples[sampleI] = y;
                }

                var startIndex = chunkI * AudioConstants.AudioPacketSamplesSize;
                playbackClip.SetData(samples, startIndex);
            }

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
