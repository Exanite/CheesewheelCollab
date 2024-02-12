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
        private float[] readBuffer = new float[1024];

        /// <summary>
        /// Buffer with data ready to be send over the network.
        /// </summary>
        private float[] sendBuffer = new float[1024];

        private void OnEnable()
        {
            // Use default microphone, with a looping 10 second buffer at 44100 Hz
            recording = Microphone.Start(null, true, 10, 44100);
        }

        private void Update()
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
                var samplesFromEnd = recording.samples - lastPosition;
                var samplesFromStart = position;

                // Read full buffers from end
                var endIterations = samplesFromEnd / readBuffer.Length;
                var remainingSamplesFromEnd = samplesFromEnd - endIterations * readBuffer.Length;
                for (var i = 0; i < endIterations; i++)
                {
                    recording.GetData(readBuffer, recording.samples - samplesFromEnd + i * readBuffer.Length);

                    readBuffer.CopyTo(sendBuffer.AsSpan());
                    SendSamples();
                }

                // Read partial buffers (combine start and end reads)

                // Read full buffers from start
            }
        }

        private void SendSamples()
        {
            Debug.Log($"Sending {sendBuffer.Length} samples");
        }
    }
}
