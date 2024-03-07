using UnityEngine;

namespace Source.Audio
{
    public abstract class AudioRecorder : MonoBehaviour
    {
        protected readonly float[] Buffer = new float[AudioConstants.SamplesChunkSize];

        /// <remarks>
        /// This might be called from a non-main thread.
        /// </remarks>
        public event SamplesAvailableCallback SamplesAvailable;

        protected void OnSamplesAvailable(int chunk, float[] buffer)
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = Mathf.Clamp(buffer[i], -1, 1);
            }

            SamplesAvailable?.Invoke(chunk, buffer);
        }
    }
}
