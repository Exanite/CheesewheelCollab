using System;
using System.Collections.Generic;
using csmatio.io;
using Exanite.Core.Numbers;
using Exanite.Core.Utilities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Source.Audio
{
    public class AudioPlayer : MonoBehaviour
    {
        [Header("Audio")]
        [FormerlySerializedAs("recorder")]
        [SerializeField] private AudioProvider audioProvider;

        // See https://trello.com/c/rQ9w7TyA/26-audio-format
        [Header("Audio Processing")]
        [Range(0, 1)]
        [SerializeField] private float volume = 1;
        [SerializeField] private int minChunksBuffered = 5;
        [SerializeField] private int maxChunksBuffered = 10;
        [SerializeField] private int minChunksQueued = 2;

        [Space]
        [SerializeField] private HrtfSubject hrtfSubject = HrtfSubject.Subject058;

        public HashSet<CustomAudioSource> AudioSources { get; set; } = new();

        private float[][] buffers;

        private float[] previousChunk = new float[AudioConstants.SamplesChunkSize];
        private float[] currentChunk = new float[AudioConstants.SamplesChunkSize];
        private float[] nextChunk = new float[AudioConstants.SamplesChunkSize];

        private float[] leftChannel = new float[AudioConstants.SamplesChunkSize];
        private float[] rightChannel = new float[AudioConstants.SamplesChunkSize];
        private float[] resultsBuffer = new float[AudioConstants.SamplesChunkSize * 2];
        private float[] outputBuffer = new float[AudioConstants.SamplesChunkSize * 2];

        private int maxReceivedChunk;
        private int lastProviderOutputChunk;

        private AudioOutput output;

        private AudioProvider previousAudioProvider;

        private Hrtf hrtf;
        private HrtfSubject previousHrtfSubject;

        private void Start()
        {
            buffers = new float[256][];
            for (var i = 0; i < buffers.Length; i++)
            {
                buffers[i] = new float[AudioConstants.SamplesChunkSize];
            }

            output = new AudioOutput(AudioConstants.SampleRate, 2);
        }

        private void OnDestroy()
        {
            output.Dispose();
        }

        private void Update()
        {
            if (audioProvider != previousAudioProvider)
            {
                if (previousAudioProvider)
                {
                    previousAudioProvider.SamplesAvailable -= OnSamplesAvailable;
                }

                if (audioProvider)
                {
                    audioProvider.SamplesAvailable += OnSamplesAvailable;
                }

                previousAudioProvider = audioProvider;
            }

            if (hrtfSubject != previousHrtfSubject)
            {
                LoadHrtf();
                previousHrtfSubject = hrtfSubject;
            }

            var queuedChunks = output.QueuedSamplesPerChannel / outputBuffer.Length;
            while (queuedChunks < minChunksQueued)
            {
                foreach (var source in AudioSources)
                {
                    if (source.Advance())
                    {
                        source.GetPrevious(previousChunk);
                        source.GetCurrent(currentChunk);
                        source.GetNext(nextChunk);

                        var results = ApplyHrtf((source.transform.position - transform.position).Swizzle(Vector3Swizzle.XZY));
                        for (var i = 0; i < results.Length; i++)
                        {
                            outputBuffer[i] += results[i];
                        }
                    }
                    else
                    {
                        Destroy(source.gameObject);
                    }

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
                    outputBuffer[i] *= volume;
                    outputBuffer[i] = Mathf.Clamp(outputBuffer[i], -1, 1);
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
