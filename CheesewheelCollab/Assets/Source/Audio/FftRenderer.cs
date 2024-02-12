using System;
using System.Linq;
using System.Numerics;
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

        private void Start()
        {
            texture = new Texture2D(500, 100, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None);
            texture.filterMode = FilterMode.Point;

            image.texture = texture;
        }

        private void Update()
        {
            var fft = recorder.Buffer.Select(y => new Complex(y, 0)).ToArray();
            Fourier.Forward(fft, FourierOptions.Default);

            var pixels = texture.GetPixels();
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }

            var frequencies = new float[texture.width];
            var maxAmplitude = 1f;
            for (var i = 0; i < frequencies.Length; i++)
            {
                frequencies[i] = GetFftAmplitude(fft, (float)i / recorder.Buffer.Length * (maxFrequency - minFrequency) + minFrequency);
                maxAmplitude = Mathf.Max(maxAmplitude, frequencies[i]);
            }

            for (var pixelX = 0; pixelX < texture.width; pixelX++)
            {
                var pixelY = (int)(texture.height * (frequencies[pixelX] / maxAmplitude));

                var index = pixelY * texture.width + pixelX;
                pixels[index] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply();
        }

        private float GetFftAmplitude(Complex[] fft, float frequency)
        {
            var index = Mathf.Clamp((int)(frequency * fft.Length / recorder.SampleRate), 0, fft.Length - 1);
            return (float)fft[index].Magnitude;
        }
    }
}
