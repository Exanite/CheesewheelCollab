using System;
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

            if (SDL.SDL_GetDefaultAudioInfo(out var defaultDeviceName, out _, 1) != 0)
            {
                throw new Exception(SDL.SDL_GetError());
            }

            Debug.Log($"Default recording devices: {defaultDeviceName}");

            if (SDL.SDL_OpenAudioDevice(defaultDeviceName, 1, ref requestedSpec, out var actualSpec, 0) == 0)
            {
                throw new Exception(SDL.SDL_GetError());
            }

            actualSpec.size.Dump();
        }
    }
}
