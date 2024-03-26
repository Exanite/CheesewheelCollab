using System;
using csmatio.io;
using UnityEngine;
using UnityEngine.Serialization;

namespace Source.Audio
{
    public class AudioPlayer : MonoBehaviour
    {
        [Header("Dependencies")]
        [FormerlySerializedAs("recorder")]
        [SerializeField] private AudioProvider audioProvider;

        [Header("Audio Override")]
        [SerializeField] private AudioClip clip;

        // See https://trello.com/c/rQ9w7TyA/26-audio-format
        [Header("Audio Processing")]
        [Range(0, 1)]
        [SerializeField] private float volume = 1;
        [SerializeField] private int minChunksBuffered = 5;
        [SerializeField] private int maxChunksBuffered = 10;
        [SerializeField] private int minChunksQueued = 2;
        [SerializeField] private HrtfSubject hrtfSubject = HrtfSubject.Subject058;

        private float[][] buffers;

        private float[] previousChunk = new float[AudioConstants.SamplesChunkSize];
        private float[] currentChunk = new float[AudioConstants.SamplesChunkSize];
        private float[] nextChunk = new float[AudioConstants.SamplesChunkSize];

        private float[] leftChannel = new float[AudioConstants.SamplesChunkSize];
        private float[] rightChannel = new float[AudioConstants.SamplesChunkSize];
        private float[] resultsBuffer = new float[AudioConstants.SamplesChunkSize * 2];
        private float[] outputBuffer = new float[AudioConstants.SamplesChunkSize * 2];

        /// <summary>
        /// Max chunk received from AudioRecorder / network
        /// </summary>
        private int maxReceivedChunk;

        /// <summary>
        /// Last chunk output to speakers. Used if audio is from <see cref="audioProvider"/>.
        /// </summary>
        private int lastProviderOutputChunk;

        private AudioOutput output;

        private Hrtf hrtf;
        private HrtfSubject loadedSubject;

        private void Start()
        {
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
            if (loadedSubject != hrtfSubject)
            {
                LoadHrtf();
                loadedSubject = hrtfSubject;
            }

            var queuedChunks = output.QueuedSamplesPerChannel / outputBuffer.Length;
            while (queuedChunks < minChunksQueued)
            {
                if (audioProvider)
                {
                    if (maxReceivedChunk - lastProviderOutputChunk > maxChunksBuffered)
                    {
                        lastProviderOutputChunk = maxReceivedChunk - maxChunksBuffered;
                    }

                    if (maxReceivedChunk - lastProviderOutputChunk > minChunksBuffered)
                    {
                        lastProviderOutputChunk++;

                        buffers[(lastProviderOutputChunk - 2 + buffers.Length) % buffers.Length].AsSpan().CopyTo(previousChunk);
                        buffers[(lastProviderOutputChunk - 1 + buffers.Length) % buffers.Length].AsSpan().CopyTo(currentChunk);
                        buffers[(lastProviderOutputChunk - 0 + buffers.Length) % buffers.Length].AsSpan().CopyTo(nextChunk);
                        var results = ApplyHrtf(new Vector3(Mathf.Cos(-Time.time), 0, Mathf.Sin(-Time.time)));

                        for (var i = 0; i < results.Length; i++)
                        {
                            outputBuffer[i] += results[i];
                        }
                    }
                }

                // --- Don't modify code below when processing audio ---
                for (var i = 0; i < outputBuffer.Length; i++)
                {
                    outputBuffer[i] = Mathf.Clamp(outputBuffer[i], -1, 1);
                    outputBuffer[i] *= volume;
                }
                output.QueueSamples(outputBuffer);
                outputBuffer.AsSpan().Clear();

                queuedChunks = output.QueuedSamplesPerChannel / outputBuffer.Length;
            }
        }

        private void OnSamplesAvailable(int chunk, float[] samples)
        {
            // Assumes chunk is strictly increasing
            maxReceivedChunk = Mathf.Max(maxReceivedChunk, chunk);
            samples.CopyTo(buffers[chunk % buffers.Length], 0);
        }

        private void LoadHrtf()
        {
            var path = Application.streamingAssetsPath + $"/CIPIC/standard_hrir_database/{hrtfSubject.ToFileName()}/hrir_final.matlab";
            hrtf = new Hrtf(new MatFileReader(path));
        }

        private float[] ApplyHrtf(Vector3 offsetToSound)
        {
            return hrtf.Apply(new ApplyHrtfOptions
            {
                OffsetToSound = offsetToSound,

                PreviousChunk = previousChunk,
                CurrentChunk = currentChunk,
                NextChunk = nextChunk,
                LeftChannel = leftChannel,
                RightChannel = rightChannel,
                ResultsBuffer = resultsBuffer,
            });
        }
    }
}
