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

        [Header("Audio Position")]
        [Range(0, 24)]
        [SerializeField] private int azimuth = 12;
        [Range(0, 49)]
        [SerializeField] private int elevation = 8;

        private float[][] buffers;
        private float[] processingBuffer;

        private float[] previousChunk = new float[AudioConstants.SamplesChunkSize];
        private float[] currentChunk = new float[AudioConstants.SamplesChunkSize];
        private float[] nextChunk = new float[AudioConstants.SamplesChunkSize];

        /// <summary>
        /// Max chunk received from AudioRecorder / network
        /// </summary>
        private int maxReceivedChunk;

        /// <summary>
        /// Last chunk output to speakers. Used if audio is from <see cref="audioProvider"/>.
        /// </summary>
        private int lastProviderOutputChunk;

        /// <summary>
        /// Last chunk output to speakers. Used if audio is from <see cref="clip"/>. Kinda hacky, but oh well.
        /// </summary>
        private int lastClipOutputChunk;

        private AudioOutput output;

        private Hrtf hrtf;
        private HrtfSubject loadedSubject;

        private void Start()
        {
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
            if (loadedSubject != hrtfSubject)
            {
                LoadHrtf();
                loadedSubject = hrtfSubject;
            }

            var queuedChunks = output.QueuedSamplesPerChannel / processingBuffer.Length;
            while (queuedChunks < minChunksQueued)
            {
                if (clip)
                {
                    ApplyHrtf();
                    lastClipOutputChunk++;
                }
                else
                {
                    if (maxReceivedChunk - lastProviderOutputChunk > maxChunksBuffered)
                    {
                        lastProviderOutputChunk = maxReceivedChunk - maxChunksBuffered;
                    }

                    if (maxReceivedChunk - lastProviderOutputChunk > minChunksBuffered)
                    {
                        lastProviderOutputChunk++;

                        //apply HRTF to audio chunk
                        ApplyHrtf();
                    }
                }

                // --- Don't modify code below when processing audio ---
                for (var i = 0; i < processingBuffer.Length; i++)
                {
                    if (Mathf.Abs(processingBuffer[i]) > 1)
                    {
                        processingBuffer[i] = Mathf.Clamp(processingBuffer[i], -1, 1);
                        Debug.LogWarning("Audio signal is greater than 1");
                    }
                    
                    processingBuffer[i] *= volume;
                }
                output.QueueSamples(processingBuffer);
                processingBuffer.AsSpan().Clear();

                queuedChunks = output.QueuedSamplesPerChannel / processingBuffer.Length;
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

        private float[] leftChannel = new float[AudioConstants.SamplesChunkSize];
        private float[] rightChannel = new float[AudioConstants.SamplesChunkSize];

        private int addend = 1;
        private int pacer = 0;
        private void ApplyHrtf()
        {
            // Todo Get position and use Hrtf to convert to indexes
            pacer++;
            if (pacer > 8)
            {
                azimuth += addend;
                pacer = 0;
            };
            if (azimuth == 25)
			{
                azimuth = 24;
                elevation = 40;
                addend = -1;
			}
            if (azimuth == -1)
			{
                azimuth = 0;
                elevation = 8;
                addend = 1;
			}

            // --- Update audio buffers ---
            UpdatePreviousChunk();
            UpdateCurrentChunk();
            UpdateNextChunk();

            // --- Apply ITD ---
            var delayInSamples = hrtf.GetItd(azimuth, elevation);

            currentChunk.AsSpan().CopyTo(leftChannel);
            currentChunk.AsSpan().CopyTo(rightChannel);

            // Add delay to start of left
            currentChunk.AsSpan().CopyTo(rightChannel);
            currentChunk.AsSpan().Slice(delayInSamples).CopyTo(leftChannel);
            nextChunk.AsSpan().Slice(0, delayInSamples).CopyTo(leftChannel.AsSpan().Slice(leftChannel.Length - delayInSamples - 1));

            // Swap buffers if needed
            if (hrtf.IsRight(azimuth))
            {
                var temp = leftChannel;
                leftChannel = rightChannel;
                rightChannel = temp;
            }

            // --- Apply HRTF ---

            var originalMaxAmplitude = 0f;
            for (var i = 0; i < AudioConstants.SamplesChunkSize; i++)
            {
                originalMaxAmplitude = Mathf.Max(originalMaxAmplitude, Mathf.Abs(currentChunk[i]));
            }

            var convolvedMaxAmplitude = 0f;

            var leftHrtf = hrtf.GetHrir(azimuth, elevation, false);
            var rightHrtf = hrtf.GetHrir(azimuth, elevation, true);

            hrtf.Convolve(previousChunk, currentChunk, nextChunk, leftHrtf).AsSpan().CopyTo(leftChannel);
            hrtf.Convolve(previousChunk, currentChunk, nextChunk, rightHrtf).AsSpan().CopyTo(rightChannel);

            for (var i = 0; i < AudioConstants.SamplesChunkSize; i++)
            {
                convolvedMaxAmplitude = Mathf.Max(convolvedMaxAmplitude, Mathf.Abs(leftChannel[i]), Mathf.Abs(rightChannel[i]));
            }

            // Reduce to original amplitude
            var amplitudeFactor = convolvedMaxAmplitude / originalMaxAmplitude;
            if (originalMaxAmplitude > 1)
            {
                // Reduce max amplitude to 1
                amplitudeFactor *= originalMaxAmplitude;
            }

            if (amplitudeFactor > 1)
            {
                for (var i = 0; i < AudioConstants.SamplesChunkSize; i++)
                {
                    leftChannel[i] /= amplitudeFactor;
                    rightChannel[i] /= amplitudeFactor;
                }
            }

            // --- Copy to output ---
            // Cannot change output size, otherwise we record and consume at different rates
            for (var i = 0; i < AudioConstants.SamplesChunkSize; i++)
            {
                // Zip left and right channels together and output
                processingBuffer[i * 2] = leftChannel[i];
                processingBuffer[i * 2 + 1] = rightChannel[i];
            }
        }

        private void UpdatePreviousChunk()
        {
            if (clip)
            {
                clip.GetData(previousChunk, (lastClipOutputChunk + 0) * AudioConstants.SamplesChunkSize % clip.samples);
            }
            else
            {
                buffers[(lastProviderOutputChunk - 2 + buffers.Length) % buffers.Length].AsSpan().CopyTo(previousChunk);
            }
        }

        private void UpdateCurrentChunk()
        {
            if (clip)
            {
                clip.GetData(currentChunk, (lastClipOutputChunk + 1) * AudioConstants.SamplesChunkSize % clip.samples);
            }
            else
            {
                buffers[(lastProviderOutputChunk - 1 + buffers.Length) % buffers.Length].AsSpan().CopyTo(currentChunk);
            }
        }

        private void UpdateNextChunk()
        {
            if (clip)
            {
                clip.GetData(nextChunk, (lastClipOutputChunk + 2) * AudioConstants.SamplesChunkSize % clip.samples);
            }
            else
            {
                buffers[(lastProviderOutputChunk - 0 + buffers.Length) % buffers.Length].AsSpan().CopyTo(nextChunk);
            }
        }
    }
}
