using SDL2;
using UnityEngine;

namespace Source.Audio
{
    public class AudioRecorder2 : MonoBehaviour
    {
        private void Start()
        {
            var requestedSpec = new SDL.SDL_AudioSpec
            {
                freq = 44100,
                format = SDL.AUDIO_F32,
                channels = 1,
                samples = 10000,
                callback = (userdata, stream, len) =>
                {

                },
            };

            SDL.SDL_OpenAudioDevice("", (int)SDL.SDL_bool.SDL_TRUE, ref requestedSpec, out var actualSpec, 0);
        }
    }
}
