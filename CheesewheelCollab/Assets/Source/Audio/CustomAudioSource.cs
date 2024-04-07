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
            Clip.GetData(buffer, (chunk + 0) * AudioConstants.SamplesChunkSize % Clip.samples);
        }

        public void GetCurrent(float[] buffer)
        {
            Clip.GetData(buffer, (chunk + 1) * AudioConstants.SamplesChunkSize % Clip.samples);
        }

        public void GetNext(float[] buffer)
        {
            Clip.GetData(buffer, (chunk + 2) * AudioConstants.SamplesChunkSize % Clip.samples);
        }
    }
}
