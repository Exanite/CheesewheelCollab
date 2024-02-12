using UnityEngine;

namespace Source.Audio
{
    public class AudioRecorder : MonoBehaviour
    {
        private AudioClip recording;

        private void OnEnable()
        {
            // Use default microphone, with a looping 10 second buffer at 44100 Hz
            recording = Microphone.Start(null, true, 10, 44100);
        }
    }
}
