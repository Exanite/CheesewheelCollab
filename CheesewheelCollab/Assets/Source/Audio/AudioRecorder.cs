using UnityEngine;

namespace Source.Audio
{
    public abstract class AudioRecorder : MonoBehaviour
    {
        public readonly float[] Buffer = new float[AudioConstants.SamplesChunkSize];

        public event SamplesAvailableCallback SamplesRecorded;

        protected void OnSamplesAvailable(int sequence, float[] buffer)
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = Mathf.Clamp(buffer[i], -1, 1);
            }

            SamplesRecorded?.Invoke(sequence, buffer);
        }
    }
}
