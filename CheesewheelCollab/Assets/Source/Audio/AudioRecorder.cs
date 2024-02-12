using System;
using UnityEngine;

namespace Source.Audio
{
    public class AudioRecorder : MonoBehaviour
    {
        private AudioClip recording;
        private int lastPosition = 0;

        public readonly int SampleRate = 10000;
        public readonly float[] Buffer = new float[250];

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

                    Buffer.CopyTo(Buffer.AsSpan());
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
            Debug.Log($"Sending {Buffer.Length} samples");
        }
    }
}
