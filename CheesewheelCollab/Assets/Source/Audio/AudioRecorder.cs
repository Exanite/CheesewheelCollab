using UnityEngine;

namespace Source.Audio
{
    public abstract class AudioRecorder : MonoBehaviour
    {
        public readonly int SampleRate = AudioConstants.RecordingSampleRate;
        public readonly float[] Buffer = new float[AudioConstants.AudioPacketSamplesSize];

        public event SamplesRecordedCallback SamplesRecorded;

        protected void OnSamplesRecorded(int sequence, float[] buffer)
        {

        }
    }
}