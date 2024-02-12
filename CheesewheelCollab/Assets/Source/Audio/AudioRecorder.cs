using System;
using UnityEngine;

namespace Source.Audio
{
    public class AudioRecorder : MonoBehaviour
    {
        private AudioClip recording;
        private int lastPosition = 0;

        /// <summary>
        /// Buffer used with <see cref="recording"/>.GetData()
        /// </summary>
        private float[] readBuffer = new float[250];

        /// <summary>
        /// Buffer with data ready to be send over the network.
        /// </summary>
        private float[] sendBuffer = new float[250];

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
                var iterations = length / readBuffer.Length;
                for (var i = 0; i < iterations; i++)
                {
                    recording.GetData(readBuffer, lastPosition + i * readBuffer.Length);

                    readBuffer.CopyTo(sendBuffer.AsSpan());
                    SendSamples();
                }

                lastPosition += iterations * readBuffer.Length;
            }
            else
            {
                // Has looped
                // Read full buffers from end
                var samplesFromEnd = recording.samples - lastPosition;
                var endIterations = samplesFromEnd / readBuffer.Length;
                for (var i = 0; i < endIterations; i++)
                {
                    recording.GetData(readBuffer, recording.samples - samplesFromEnd + i * readBuffer.Length);

                    readBuffer.CopyTo(sendBuffer.AsSpan());
                    SendSamples();
                }

                lastPosition = 0;

                // Read full buffers from start
                ReadSamples();
            }
        }

        private void SendSamples()
        {
            Debug.Log($"Sending {sendBuffer.Length} samples");
        }
    }
}
