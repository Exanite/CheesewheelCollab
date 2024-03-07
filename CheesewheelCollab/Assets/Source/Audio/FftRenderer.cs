using System;
using System.Linq;
using System.Numerics;
using Exanite.Core.Utilities;
using MathNet.Numerics.IntegralTransforms;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

namespace Source.Audio
{
    public class FftRenderer : MonoBehaviour
    {
        [SerializeField] private AudioRecorder recorder;
        [SerializeField] private RawImage image;

        [SerializeField] private int minFrequency = 0;
        [SerializeField] private int maxFrequency = 20000;

        private Texture2D texture;
        private float[] buffer;

        private void Start()
        {
            texture = new Texture2D(AudioConstants.SamplesChunkSize, 100, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None);
            texture.filterMode = FilterMode.Point;

            image.texture = texture;

            recorder.SamplesAvailable += (_, samples) => buffer = samples;
        }

        private void Update()
        {
            if (buffer == null)
            {
                return;
            }

            var fft = buffer.Select(y => new Complex(y, 0)).ToArray();
            Fourier.Forward(fft, FourierOptions.Default);

            var pixels = texture.GetPixels();
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }

            var frequencies = new float[buffer.Length];
            var maxAmplitude = 1f;
            for (var i = 0; i < buffer.Length; i++)
            {
                frequencies[i] = GetFrequencyAmplitude(fft, MathUtility.Remap((float)i / buffer.Length, 0, 1, minFrequency, maxFrequency));
                maxAmplitude = Mathf.Max(maxAmplitude, frequencies[i]);
            }

            for (var pixelX = 0; pixelX < buffer.Length; pixelX++)
            {
                var pixelY = Mathf.Clamp((int)(texture.height * (frequencies[pixelX] / maxAmplitude)), 0, texture.height - 1);

                var index = pixelY * texture.width + pixelX;
                pixels[index] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply();
        }

        private float GetFrequencyAmplitude(Complex[] fft, float frequency)
        {
            var index = Mathf.Clamp((int)(frequency * fft.Length / AudioConstants.SampleRate), 0, fft.Length - 1);
            return (float)fft[index].Magnitude;
        }
    }
}
