using UnityEngine;

namespace Source.Audio
{
    public class AudioRecorder : MonoBehaviour
    {
        public delegate void SamplesRecordedCallback(int sequence, float[] samples);

        private AudioClip recording;
        private int lastPosition = 0;
        private int lastSequence = 0;

        public readonly int SampleRate = AudioConstants.RecordingSampleRate;
        public readonly float[] Buffer = new float[AudioConstants.AudioPacketSamplesSize];

        public event SamplesRecordedCallback SamplesRecorded;

        private void OnEnable()
        {
            // Use default microphone, with a looping 10 second buffer at 10000 Hz
            recording = Microphone.Start(null, true, 10, SampleRate);
        }

        private void Update()
        {
            ReadSamples();
        }

        private void ReadSamples()
        {
            var position = Microphone.GetPosition(null);
            if (position > lastPosition)
            {
                // Hasn't looped yet
                var length = position - lastPosition;
                var iterations = length / Buffer.Length;
                for (var i = 0; i < iterations; i++)
                {
                    recording.GetData(Buffer, lastPosition + i * Buffer.Length);

                    SendSamples();
                }

                lastPosition += iterations * Buffer.Length;
            }
            else
            {
                // Has looped
                // Read full buffers from end
                var samplesFromEnd = recording.samples - lastPosition;
                var endIterations = samplesFromEnd / Buffer.Length;
                for (var i = 0; i < endIterations; i++)
                {
                    recording.GetData(Buffer, recording.samples - samplesFromEnd + i * Buffer.Length);
                    SendSamples();
                }

                lastPosition = 0;

                // Read full buffers from start
                ReadSamples();
            }
        }

        private void SendSamples()
        {
            SamplesRecorded?.Invoke(lastSequence++, Buffer);
        }
    }
}
