using System;
using System.Collections.Generic;
using Exanite.Core.Utilities;
using SDL2;
using Source.Sdl;
using UnityEngine;

namespace Source.Audio
{
    public class AudioOutput : IDisposable
    {
        private bool isDisposed = false;

        private SDL.SDL_AudioSpec spec;
        private uint deviceId;

        private readonly int sampleRate;

        public AudioOutput(int sampleRate)
        {
            SdlContext.Start();

            this.sampleRate = sampleRate;
            spec = new SDL.SDL_AudioSpec
            {
                freq = sampleRate,
                format = SDL.AUDIO_F32,
                channels = 1,
                samples = AudioConstants.SamplesChunkSize,
            };

            var deviceNames = new List<string>();
            var deviceCount = SDL.SDL_GetNumAudioDevices(1);
            for (var i = 0; i < deviceCount; i++)
            {
                deviceNames.Add(SDL.SDL_GetAudioDeviceName(i, 0));
            }
            Debug.Log($"Available playback devices: {DebugUtility.Format(deviceNames)}");

            if (SDL.SDL_GetDefaultAudioInfo(out var defaultDeviceName, out _, 0) != 0)
            {
                throw new Exception(SDL.SDL_GetError());
            }
            Debug.Log($"Using default playback device: {defaultDeviceName}");

            deviceId = SDL.SDL_OpenAudioDevice(defaultDeviceName, 0, ref spec, out var actualSpec, 0);
            if (deviceId == 0)
            {
                throw new Exception(SDL.SDL_GetError());
            }

            spec = actualSpec;

            SDL.SDL_PauseAudioDevice(deviceId, 0);
        }

        public int QueuedSampleCount => (int)(SDL.SDL_GetQueuedAudioSize(deviceId) / sizeof(float));

        public unsafe void QueueSamples(Span<float> samples)
        {
            fixed (float* samplesP = samples)
            {
                SDL.SDL_QueueAudio(deviceId, (IntPtr)samplesP, (uint)(samples.Length * sizeof(float)));
            }
        }

        private void ReleaseUnmanagedResources()
        {
            SDL.SDL_CloseAudioDevice(deviceId);
            SdlContext.Stop();
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);

            isDisposed = true;
        }

        ~AudioOutput()
        {
            ReleaseUnmanagedResources();
        }
    }
}
