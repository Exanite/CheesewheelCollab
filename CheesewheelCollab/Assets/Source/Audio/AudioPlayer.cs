using System;
using csmatio.io;
using UnityEngine;
using UnityEngine.Serialization;

namespace Source.Audio
{
    public class AudioPlayer : MonoBehaviour
    {
        [FormerlySerializedAs("recorder")]
        [Header("Dependencies")]
        [SerializeField] private AudioProvider audioProvider;

        // See https://trello.com/c/rQ9w7TyA/26-audio-format
        [Header("Settings")]
        [SerializeField] private int minChunksBuffered = 5;
        [SerializeField] private int maxChunksBuffered = 10;
        [SerializeField] private int minChunksQueued = 2;

        private float[][] buffers;
        private float[] processingBuffer;

        /// <summary>
        /// Max chunk received from AudioRecorder / network
        /// </summary>
        private int maxReceivedChunk;

        /// <summary>
        /// Last chunk output to speakers
        /// </summary>
        private int lastOutputChunk;

        private AudioOutput output;

        private Hrtf hrtf;

        private void Start()
        {
            LoadHRTF();

            processingBuffer = new float[AudioConstants.SamplesChunkSize * 2];
            buffers = new float[256][];
            for (var i = 0; i < buffers.Length; i++)
            {
                buffers[i] = new float[AudioConstants.SamplesChunkSize];
            }

            audioProvider.SamplesAvailable += OnSamplesAvailable;

            output = new AudioOutput(AudioConstants.SampleRate, 2);
        }

        private void OnDestroy()
        {
            output.Dispose();
        }

        private void Update()
        {
            var queuedChunks = output.QueuedSamplesPerChannel / processingBuffer.Length;
            if (queuedChunks < minChunksQueued)
            {
                if (maxReceivedChunk - lastOutputChunk > maxChunksBuffered)
                {
                    lastOutputChunk = maxReceivedChunk - maxChunksBuffered;
                }

                if (maxReceivedChunk - lastOutputChunk > minChunksBuffered)
                {
                    lastOutputChunk++;

                    //apply HRTF to audio chunk
                    ApplyHrtf();
                }

                // Don't modify code below when processing audio
                for (var i = 0; i < processingBuffer.Length; i++)
                {
                    processingBuffer[i] = Mathf.Clamp(processingBuffer[i], -1, 1);
                }

                output.QueueSamples(processingBuffer);
                processingBuffer.AsSpan().Clear();
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
            var path = Application.streamingAssetsPath + "/HRTFs/hrir58.mat";
            hrtf = new Hrtf(new MatFileReader(path));
        }

        private float[] leftChannel = new float[AudioConstants.SamplesChunkSize];
        private float[] rightChannel = new float[AudioConstants.SamplesChunkSize];
        private void ApplyHrtf()
        {
            // Todo Get position and use Hrtf to convert to indexes

            var azimuth = (int)(Time.time * 10 % 25);
            var elevation = 8;

            // --- Apply ITD ---
            var delayInSamples = hrtf.GetItd(azimuth, elevation);
            var current = buffers[(lastOutputChunk - 1 + buffers.Length) % buffers.Length];
            var next = buffers[(lastOutputChunk - 0 + buffers.Length) % buffers.Length];

            current.AsSpan().CopyTo(leftChannel);
            current.AsSpan().CopyTo(rightChannel);

            // Add delay to start of left
            current.AsSpan().CopyTo(rightChannel);
            current.AsSpan().Slice(delayInSamples).CopyTo(leftChannel);
            next.AsSpan().Slice(0, delayInSamples).CopyTo(leftChannel.AsSpan().Slice(leftChannel.Length - delayInSamples - 1));

            // Swap buffers if needed
            if (hrtf.IsRight(azimuth))
            {
                var temp = leftChannel;
                leftChannel = rightChannel;
                rightChannel = temp;
            }

            // --- Apply HRTF ---

            var leftHrtf = hrtf.GetHrtf(azimuth, elevation, false);
            var rightHrtf = hrtf.GetHrtf(azimuth, elevation, true);

            // hrtf.Convolve(leftChannel, leftHrtf).AsSpan().CopyTo(leftChannel);
            // hrtf.Convolve(rightChannel, rightHrtf).AsSpan().CopyTo(rightChannel);

            // --- Copy to output ---
            // Cannot change output size, otherwise we record and consume at different rates
            for (var i = 0; i < AudioConstants.SamplesChunkSize; i++)
            {
                // Zip left and right channels together and output
                processingBuffer[i * 2] = leftChannel[i];
                processingBuffer[i * 2 + 1] = rightChannel[i];
            }
        }
    }
}
