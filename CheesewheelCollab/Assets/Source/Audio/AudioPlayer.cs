using System;
using UnityEngine;
using csmatio.types;
using csmatio.io;

namespace Source.Audio
{
    public class AudioPlayer : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private AudioRecorder recorder;

        // Total delay = network delay + buffer + queue (need to validate this - William)
        [Header("Settings")]
        [Tooltip("Audio will be buffered for this long before being used. Lower numbers = less delay, but can lead to incomplete data => silence.")]
        private float bufferSeconds = 1;
        // Guess: This probably can be kept fairly low (maybe 4 frames, even less if processed on background thread)
        [Tooltip("This amount of audio data will be kept in the queue. Lower numbers = less latency, but can lead to audio popping.")]
        private float queueSeconds = 0.5f;

        // Currently 256 buffers * 500 samples per buffer / 10000 Hz = 12.8 seconds of buffers.
        // Window must be <= 12.8 / 2, therefore we can have 6.4 seconds of buffering.
        // This means we can have a max delay of 6.4 seconds. Our min delay is 0 seconds, but that can cause issues.
        private float[][] buffers;
        private float[] activeBuffer;

        /// <summary>
        /// Max sequence received from AudioRecorder / network
        /// </summary>
        private int maxReceivedSequence;

        /// <summary>
        /// Last sequence output to speakers
        /// </summary>
        private int lastOutputSequence;

        private AudioOutput output;

        private void Start()
        {
            LoadHRTF();

            activeBuffer = new float[AudioConstants.SamplesChunkSize];
            buffers = new float[256][];
            for (var i = 0; i < buffers.Length; i++)
            {
                buffers[i] = new float[AudioConstants.SamplesChunkSize];
            }

            recorder.SamplesAvailable += OnSamplesAvailable;

            output = new AudioOutput(AudioConstants.SampleRate, 1);
        }

        private void OnDestroy()
        {
            output.Dispose();
        }

        // private int sineSequence;

        private void Update()
        {
            var queuedSamples = output.QueuedSamplesPerChannel;
            var targetQueuedSamples = AudioConstants.SampleRate * queueSeconds;

            if (queuedSamples < targetQueuedSamples)
            {
                // Stay at most 10 sequences behind
                if (maxReceivedSequence - lastOutputSequence > 10)
                {
                    lastOutputSequence = maxReceivedSequence - 5;
                }

                // Stay at least 5 sequences behind
                if (lastOutputSequence + 5 < maxReceivedSequence)
                {
                    lastOutputSequence++;
                    buffers[lastOutputSequence % buffers.Length].CopyTo(activeBuffer, 0);
                }

                // // Sine wave output (sounds like an organ)
                // for (var i = 0; i < activeBuffer.Length; i++)
                // {
                //     var time = (float)(sineSequence * activeBuffer.Length + i) / AudioConstants.SampleRate;
                //
                //     activeBuffer[i] += 0.025f * Mathf.Sin(Mathf.Sin(2 * Mathf.PI * 220 * time) + 2 * Mathf.PI * 220 * time);
                //     activeBuffer[i] += 0.025f * Mathf.Sin(Mathf.Sin(2 * Mathf.PI * 440 * time) + 2 * Mathf.PI * 440 * time);
                //     activeBuffer[i] += 0.025f * Mathf.Sin(Mathf.Sin(2 * Mathf.PI * 880 * time) + 2 * Mathf.PI * 880 * time);
                //     activeBuffer[i] += 0.025f * Mathf.Sin(Mathf.Sin(2 * Mathf.PI * 1760 * time) + 2 * Mathf.PI * 1760 * time);
                // }
                // sineSequence++;

                // Don't modify code below when processing audio
                for (var i = 0; i < activeBuffer.Length; i++)
                {
                    activeBuffer[i] = Mathf.Clamp(activeBuffer[i], -1, 1);
                }
                output.QueueSamples(activeBuffer);
                activeBuffer.AsSpan().Clear();
            }
        }

        private void OnSamplesAvailable(int sequence, float[] samples)
        {
            // Assumes sequence is strictly increasing
            maxReceivedSequence = Mathf.Max(maxReceivedSequence, sequence);
            samples.CopyTo(buffers[sequence % buffers.Length], 0);
        }

        private void LoadHRTF()
        {
            //HRTF measured at 25 azimuth points (1st dim), 50 elevation points (2nd dim),
            //  all at 5 degrees offset from the next point
            string path = Application.dataPath + "/Content/HRTFs/hrir58.mat";

            MatFileReader mfr = new MatFileReader(path);

            Debug.Log(mfr.MatFileHeader.ToString());
            foreach (MLArray mla in mfr.Data)
            {
                //Debug.Log(mla.ContentToString() + "\n");
            }

            Debug.Log(mfr.Data[1].ContentToString() + "\n");
            double[][] mld = ((MLDouble)mfr.Data[1]).GetArray();
            Debug.Log(mld[0][0]);
        }
    }
}
