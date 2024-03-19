using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Source.Audio
{
    public class AudioRenderer : MonoBehaviour
    {
        [FormerlySerializedAs("recorder")]
        [SerializeField] private AudioProvider audioProvider;
        [SerializeField] private RawImage image;

        private Texture2D texture;
        private float[] buffer;

        private void Start()
        {
            audioProvider.SamplesAvailable += (_, samples) => buffer = samples;
        }

        private void Update()
        {
            if (buffer == null)
            {
                return;
            }

            if (!texture || texture.width != buffer.Length)
            {
                Destroy(texture);

                texture = new Texture2D(buffer.Length, 100, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None);
                texture.filterMode = FilterMode.Point;

                image.texture = texture;
            }

            var pixels = texture.GetPixels();
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }

            for (var pixelX = 0; pixelX < buffer.Length; pixelX++)
            {
                var y = buffer[pixelX];
                var pixelY = Mathf.Clamp((int)(texture.height * ((y + 1) / 2)), 0, texture.height - 1);

                var index = pixelY * texture.width + pixelX;
                pixels[index] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply();
        }
    }
}
