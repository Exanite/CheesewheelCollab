using UnityEngine;
using UnityEngine.Assertions;

namespace Source.Audio
{
    public class ActiveAudioClip
    {
        public AudioClip Clip { get; set; }

        public float Volume { get; set; } = 1;
        public Vector3 Position { get; set; }

        public bool Loop { get; set; }
        public int Chunk { get; set; }

        public ActiveAudioClip(AudioClip clip)
        {
            Assert.AreEqual(1, clip.channels);
            Assert.AreEqual(AudioConstants.SampleRate, clip.frequency);

            Clip = clip;
        }

        public bool Advance()
        {
            Chunk++;

            if (!Loop && Chunk * AudioConstants.SamplesChunkSize > Clip.samples)
            {
                return false;
            }

            return true;
        }

        public void GetPrevious(float[] buffer)
        {
            Clip.GetData(buffer, (Chunk + 0) * AudioConstants.SamplesChunkSize % Clip.samples);
        }

        public void GetCurrent(float[] buffer)
        {
            Clip.GetData(buffer, (Chunk + 1) * AudioConstants.SamplesChunkSize % Clip.samples);
        }

        public void GetNext(float[] buffer)
        {
            Clip.GetData(buffer, (Chunk + 2) * AudioConstants.SamplesChunkSize % Clip.samples);
        }
    }
}
