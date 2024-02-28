using System.Collections.Generic;
using Exanite.Core.Utilities;
using SDL2;
using UnityEngine;

namespace Source.Audio
{
    public class AudioRecorder2 : MonoBehaviour
    {
        private void Start()
        {
            SDL.SDL_Init(SDL.SDL_INIT_AUDIO);
            var requestedSpec = new SDL.SDL_AudioSpec
            {
                freq = 44100,
                format = SDL.AUDIO_F32,
                channels = 1,
                samples = 250,
                callback = (userdata, stream, len) =>
                {
                    "Test".Dump();
                },
            };

            var deviceNames = new List<string>();
            var deviceCount = SDL.SDL_GetNumAudioDevices(1);
            for (var i = 0; i < deviceCount; i++)
            {
                deviceNames.Add(SDL.SDL_GetAudioDeviceName(i, 1));
            }

            Debug.Log($"Available recording devices: {DebugUtility.Format(deviceNames)}");

            var deviceId = (int)SDL.SDL_OpenAudioDevice("", 1, ref requestedSpec, out var actualSpec, 0);
            var deviceName = SDL.SDL_GetAudioDeviceName(deviceId, 1);
            Debug.Log($"Recording using {deviceName}");

            actualSpec.size.Dump();
        }
    }
}
