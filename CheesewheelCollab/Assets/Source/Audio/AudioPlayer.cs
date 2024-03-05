using System;
using System.Collections.Generic;
using UnityEngine;
using csmatio.types;
using csmatio.io;
using Exanite.Core.Utilities;
using SDL2;
using Source.Sdl;

namespace Source.Audio
{
    public class AudioPlayer : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private AudioRecorder recorder;

        [Header("Settings")]
        private float delaySeconds = 1;

        private float[][] buffers;

        // This callback is accessed by native C code
        // Must save callback to field so it doesn't get GCed and cause a segfault
        private SDL.SDL_AudioCallback audioCallback;
        private SDL.SDL_AudioSpec spec;
        private uint deviceId;
        private int sequence;

        private void Start()
        {
            LoadHRTF();

            buffers = new float[256][];
            for (var i = 0; i < buffers.Length; i++)
            {
                buffers[i] = new float[AudioConstants.AudioPacketSamplesSize];
            }

            recorder.SamplesRecorded += OnSamplesRecorded;

            SdlContext.Start();

            var sampleRate = AudioConstants.RecordingSampleRate;
            audioCallback = (userdata, stream, len) =>
            {
                unsafe
                {
                    var streamData = new Span<float>((void*)stream, len / sizeof(float));
                    streamData.Clear(); // For safety

                    for (var i = 0; i < streamData.Length; i++)
                    {
                        var time = (float)(i + sequence * streamData.Length) / sampleRate;
                        streamData[i] = Mathf.Sin(2 * Mathf.PI * 440 * time);
                    }

                    sequence++;
                }
            };

            spec = new SDL.SDL_AudioSpec
            {
                freq = sampleRate,
                format = SDL.AUDIO_F32,
                channels = 1,
                samples = AudioConstants.AudioPacketSamplesSize,
                callback = audioCallback,
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

        private void OnDestroy()
        {
            if (deviceId != 0)
            {
                SDL.SDL_CloseAudioDevice(deviceId);
                deviceId = 0;
            }

            SdlContext.Stop();
        }

        private void OnSamplesRecorded(int sequence, float[] samples)
        {
            samples.CopyTo(buffers[sequence % buffers.Length], 0);
        }

        private void LoadHRTF()
        {
            //HRTF measured at 25 azimuth points (1st dim), 50 elevation points (2nd dim),
            //  all at 5 degrees offset from the next point
            string path = Application.dataPath + "/Content/HRTFs/hrir58.mat";

            MatFileReader mfr = new MatFileReader(path);

            Debug.Log(mfr.MatFileHeader.ToString());
            foreach (MLArray mla in mfr.Data)
            {
                //Debug.Log(mla.ContentToString() + "\n");
            }

            Debug.Log(mfr.Data[1].ContentToString() + "\n");
            double[][] mld = ((MLDouble)mfr.Data[1]).GetArray();
            Debug.Log(mld[0][0]);
        }
    }
}
