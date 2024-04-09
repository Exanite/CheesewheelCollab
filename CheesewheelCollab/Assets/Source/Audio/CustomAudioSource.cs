using System;
using UnityEngine;
using UnityEngine.Assertions;

namespace Source.Audio
{
    public class CustomAudioSource : MonoBehaviour
    {
        [Header("Required")]
        [SerializeField] private AudioClip clip;

        [Header("Settings")]
        [Range(0, 1)]
        [SerializeField] private float volume = 1;
        [SerializeField] private bool loop;
        [SerializeField] private AnimationCurve attenuationCurve;
        [SerializeField] private float attenuationStart;
        [SerializeField] private float attenuationEnd;

        private int chunk;

        public AudioClip Clip
        {
            get => clip;
            set => clip = value;
        }

        public float Volume
        {
            get => volume;
            set => volume = value;
        }

        public bool Loop
        {
            get => loop;
            set => loop = value;
        }

        public AnimationCurve AttenuationCurve
        {
            get => attenuationCurve;
            set => attenuationCurve = value;
        }

        public float AttenuationStart
        {
            get => attenuationStart;
            set => attenuationStart = value;
        }

        public float AttenuationEnd
        {
            get => attenuationEnd;
            set => attenuationEnd = value;
        }

        private void OnEnable()
        {
            Assert.AreEqual(1, Clip.channels);
            Assert.AreEqual(AudioConstants.SampleRate, Clip.frequency);

            foreach (var player in FindObjectsOfType<AudioPlayer>())
            {
                player.AudioSources.Add(this);
            }
        }

        private void OnDisable()
        {
            foreach (var player in FindObjectsOfType<AudioPlayer>())
            {
                player.AudioSources.Remove(this);
            }
        }

        public bool Advance()
        {
            chunk++;

            if (!Loop && chunk * AudioConstants.SamplesChunkSize > Clip.samples)
            {
                return false;
            }

            return true;
        }

        public void GetPrevious(float[] buffer)
        {
            var readStart = (chunk + 0) * AudioConstants.SamplesChunkSize;
            Clip.GetData(buffer, readStart % Clip.samples);

            if (!loop && readStart + AudioConstants.SamplesChunkSize > Clip.samples)
            {
                var remainingSamples = clip.samples - readStart;
                if (remainingSamples > 0)
                {
                    var invalid = buffer.AsSpan().Slice(remainingSamples);
                    invalid.Clear();
                }
                else
                {
                    buffer.AsSpan().Clear();
                }
            }
        }

        public void GetCurrent(float[] buffer)
        {
            var readStart = (chunk + 1) * AudioConstants.SamplesChunkSize;
            Clip.GetData(buffer, readStart % Clip.samples);

            if (!loop && readStart + AudioConstants.SamplesChunkSize > Clip.samples)
            {
                var remainingSamples = clip.samples - readStart;
                if (remainingSamples > 0)
                {
                    var invalid = buffer.AsSpan().Slice(remainingSamples);
                    invalid.Clear();
                }
                else
                {
                    buffer.AsSpan().Clear();
                }
            }
        }

        public void GetNext(float[] buffer)
        {
            var readStart = (chunk + 2) * AudioConstants.SamplesChunkSize;
            Clip.GetData(buffer, readStart % Clip.samples);

            if (!loop && readStart + AudioConstants.SamplesChunkSize > Clip.samples)
            {
                var remainingSamples = clip.samples - readStart;
                if (remainingSamples > 0)
                {
                    var invalid = buffer.AsSpan().Slice(remainingSamples);
                    invalid.Clear();
                }
                else
                {
                    buffer.AsSpan().Clear();
                }
            }
        }
    }
}
