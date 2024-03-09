using System;
using UnityEngine;
using csmatio.types;
using csmatio.io;
using Exanite.Core.Utilities;

namespace Source.Audio
{
    public class AudioPlayer : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private AudioRecorder recorder;

        // See https://trello.com/c/rQ9w7TyA/26-audio-format
        [Header("Settings")]
        [SerializeField] private int minChunksBuffered = 5;
        [SerializeField] private int maxChunksBuffered = 10;
        [SerializeField] private int minChunksQueued = 2;

        // Currently 256 buffers * 500 samples per buffer / 10000 Hz = 12.8 seconds of buffers.
        // Window must be <= 12.8 / 2, therefore we can have 6.4 seconds of buffering.
        // This means we can have a max delay of 6.4 seconds. Our min delay is 0 seconds, but that can cause issues.
        private float[][] buffers;
        private float[] activeBuffer;

        /// <summary>
        /// Max chunk received from AudioRecorder / network
        /// </summary>
        private int maxReceivedChunk;

        /// <summary>
        /// Last chunk output to speakers
        /// </summary>
        private int lastOutputChunk;

        private AudioOutput output;

        private void Start()
        {
            LoadHRTF();

            activeBuffer = new float[(int)(AudioConstants.SamplesChunkSize * ((float)AudioConstants.PlaybackSampleRate / AudioConstants.RecordingSampleRate))];
            buffers = new float[256][];
            for (var i = 0; i < buffers.Length; i++)
            {
                buffers[i] = new float[AudioConstants.SamplesChunkSize];
            }

            recorder.SamplesAvailable += OnSamplesAvailable;

            output = new AudioOutput(AudioConstants.PlaybackSampleRate, 1);
        }

        private void OnDestroy()
        {
            output.Dispose();
        }

        // private int sineChunk;

        private void Update()
        {
            var queuedChunks = output.QueuedSamplesPerChannel / AudioConstants.SamplesChunkSize;
            if (queuedChunks < minChunksQueued)
            {
                if (maxReceivedChunk - lastOutputChunk > maxChunksBuffered)
                {
                    lastOutputChunk = maxReceivedChunk - maxChunksBuffered;
                }

                if (maxReceivedChunk - lastOutputChunk > minChunksBuffered)
                {
                    lastOutputChunk++;

                    // Bad resampling algorithm
                    var recordingSamples0 = buffers[lastOutputChunk % buffers.Length];
                    var recordingSamples1 = buffers[(lastOutputChunk - 1 + buffers.Length) % buffers.Length];
                    var recordingSamples2 = buffers[(lastOutputChunk - 1 + buffers.Length) % buffers.Length];
                    for (var i = 0; i < activeBuffer.Length; i++)
                    {
                        var recordingSamplesI = MathUtility.Remap(i, 0, activeBuffer.Length - 1, 0, AudioConstants.SamplesChunkSize - 1);

                        var sample0 = Mathf.FloorToInt(recordingSamplesI) - 1;

                        var y0 = sample0 + 0 < 0 ? recordingSamples2[sample0 + 0 + AudioConstants.SamplesChunkSize] : recordingSamples1[sample0 + 0];
                        var y1 = sample0 + 1 < 0 ? recordingSamples2[sample0 + 1 + AudioConstants.SamplesChunkSize] : recordingSamples1[sample0 + 1];
                        var y2 = sample0 + 2 >= AudioConstants.SamplesChunkSize ? recordingSamples2[sample0 + 2 - AudioConstants.SamplesChunkSize] : recordingSamples1[sample0 + 2];
                        var y3 = sample0 + 3 >= AudioConstants.SamplesChunkSize ? recordingSamples0[sample0 + 3 - AudioConstants.SamplesChunkSize] : recordingSamples1[sample0 + 3];

                        var interpolated = CubicInterpolate(y0, y1, y2, y3, recordingSamplesI % 1);

                        activeBuffer[i] = interpolated;
                    }
                }

                // // Sine wave output (sounds like an organ)
                // for (var i = 0; i < activeBuffer.Length; i++)
                // {
                //     var time = (float)(sineChunk * activeBuffer.Length + i) / AudioConstants.SampleRate;
                //
                //     activeBuffer[i] += 0.025f * Mathf.Sin(Mathf.Sin(2 * Mathf.PI * 220 * time) + 2 * Mathf.PI * 220 * time);
                //     activeBuffer[i] += 0.025f * Mathf.Sin(Mathf.Sin(2 * Mathf.PI * 440 * time) + 2 * Mathf.PI * 440 * time);
                //     activeBuffer[i] += 0.025f * Mathf.Sin(Mathf.Sin(2 * Mathf.PI * 880 * time) + 2 * Mathf.PI * 880 * time);
                //     activeBuffer[i] += 0.025f * Mathf.Sin(Mathf.Sin(2 * Mathf.PI * 1760 * time) + 2 * Mathf.PI * 1760 * time);
                // }
                // sineChunk++;

                // Don't modify code below when processing audio
                for (var i = 0; i < activeBuffer.Length; i++)
                {
                    activeBuffer[i] = Mathf.Clamp(activeBuffer[i], -1, 1);
                }
                output.QueueSamples(activeBuffer);
                activeBuffer.AsSpan().Clear();
            }
        }

        private void OnSamplesAvailable(int chunk, float[] samples)
        {
            // Assumes chunk is strictly increasing
            maxReceivedChunk = Mathf.Max(maxReceivedChunk, chunk);
            samples.CopyTo(buffers[chunk % buffers.Length], 0);
        }

        private void LoadHRTF()
        {
            //HRTF measured at 25 azimuth points (1st dim), 50 elevation points (2nd dim),
            //  all at 5 degrees offset from the next point
            string path = Application.streamingAssetsPath + "/HRTFs/hrir58.mat";

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

        private float CubicInterpolate(
            float y0,
            float y1,
            float y2,
            float y3,
            float mu)
        {
            var mu2 = mu * mu;
            var a0 = y3 - y2 - y0 + y1;
            var a1 = y0 - y1 - a0;
            var a2 = y2 - y0;
            var a3 = y1;

            return a0 * mu * mu2 + a1 * mu2 + a2 * mu + a3;
        }
    }
}
