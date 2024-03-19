using UnityEngine;

namespace Source.Audio
{
    public class UnityAudioClipProvider : AudioProvider
    {
        [SerializeField] private AudioClip clip;

        private int chunk;
        private float elapsedTime;
        private float[] samples;

        private void Start()
        {
            var length = clip.samples;
            samples = new float[length];
            clip.GetData(samples, 0);
        }

        protected override void Update()
        {
            var stepTime = (float)AudioConstants.SamplesChunkSize / AudioConstants.SampleRate;

            elapsedTime += Time.deltaTime;
            while (elapsedTime >= stepTime)
            {
                var startPosition = chunk * AudioConstants.SamplesChunkSize;
                for (var i = 0; i < AudioConstants.SamplesChunkSize; i++)
                {
                    Buffer[i] = samples[(startPosition + i) % samples.Length];
                }

                OnSamplesAvailable(chunk, Buffer);

                elapsedTime -= stepTime;
                chunk++;
            }

            base.Update();
        }
    }
}
