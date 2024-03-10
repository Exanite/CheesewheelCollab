using System;
using System.Collections.Generic;
using Exanite.Core.Utilities;
using SDL2;
using Source.Sdl;
using UnityEngine;

namespace Source.Audio
{
    public class SdlAudioRecorder : AudioRecorder
    {
        private int chunk = 0;

        // This callback is accessed by native C code
        // Must save callback to field so it doesn't get GCed and cause a segfault
        private SDL.SDL_AudioCallback audioCallback;
        private SDL.SDL_AudioSpec spec;
        private uint deviceId;

        private void Start()
        {
            SdlContext.Start();

            audioCallback = (userdata, stream, len) =>
            {
                unsafe
                {
                    var streamData = new Span<float>((void*)stream, len / sizeof(float));
                    streamData.CopyTo(Buffer);

                    OnSamplesAvailable(chunk++, Buffer);
                }
            };

            spec = new SDL.SDL_AudioSpec
            {
                freq = AudioConstants.SampleRate,
                format = SDL.AUDIO_F32,
                channels = 1,
                samples = AudioConstants.SamplesChunkSize,
                callback = audioCallback,
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

            Debug.Log($"Using default recording device: {defaultDeviceName}");

            deviceId = SDL.SDL_OpenAudioDevice(defaultDeviceName, 1, ref spec, out var actualSpec, 0);
            if (deviceId == 0)
            {
                throw new Exception(SDL.SDL_GetError());
            }

            spec = actualSpec;

            SDL.SDL_PauseAudioDevice(deviceId, 0);
        }

        private void OnDestroy()
        {
            if (deviceId != 0)
            {
                SDL.SDL_CloseAudioDevice(deviceId);
                deviceId = 0;
            }

            SdlContext.Stop();
        }
    }
}
