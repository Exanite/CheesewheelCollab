using System;
using System.Collections.Generic;
using Exanite.Core.Utilities;
using SDL2;
using UnityEngine;

namespace Source.Audio
{
    public class SdlAudioRecorder : MonoBehaviour
    {
        private uint deviceId = 0;

        private void Start()
        {
            if (SDL.SDL_Init(SDL.SDL_INIT_AUDIO) != 0)
            {
                throw new Exception(SDL.SDL_GetError());
            }

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

            deviceId = SDL.SDL_OpenAudioDevice(defaultDeviceName, 1, ref requestedSpec, out var actualSpec, 0);
            if (deviceId == 0)
            {
                throw new Exception(SDL.SDL_GetError());
            }

            SDL.SDL_PauseAudioDevice(deviceId, 0);
        }

        private void OnDestroy()
        {
            if (deviceId != 0)
            {
                SDL.SDL_CloseAudioDevice(deviceId);
                deviceId = 0;
            }
        }
    }
}
