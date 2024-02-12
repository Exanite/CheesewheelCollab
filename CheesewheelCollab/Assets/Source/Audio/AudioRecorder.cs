using System;
using UnityEngine;

namespace Source.Audio
{
    public class AudioRecorder : MonoBehaviour
    {
        private AudioClip recording;
        private int lastPosition = 0;

        private float[] buffer = new float[250];

        private void OnEnable()
        {
            // Use default microphone, with a looping 10 second buffer at 10000 Hz
            recording = Microphone.Start(null, true, 10, 10000);
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
                var iterations = length / buffer.Length;
                for (var i = 0; i < iterations; i++)
                {
                    recording.GetData(buffer, lastPosition + i * buffer.Length);

                    buffer.CopyTo(buffer.AsSpan());
                    SendSamples();
                }

                lastPosition += iterations * buffer.Length;
            }
            else
            {
                // Has looped
                // Read full buffers from end
                var samplesFromEnd = recording.samples - lastPosition;
                var endIterations = samplesFromEnd / buffer.Length;
                for (var i = 0; i < endIterations; i++)
                {
                    recording.GetData(buffer, recording.samples - samplesFromEnd + i * buffer.Length);
                    SendSamples();
                }

                lastPosition = 0;

                // Read full buffers from start
                ReadSamples();
            }
        }

        private void SendSamples()
        {
            Debug.Log($"Sending {buffer.Length} samples");
        }
    }
}
