using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;

namespace Source.Audio
{
    public class AudioRenderer : MonoBehaviour
    {
        [SerializeField] private AudioRecorder recorder;
        [SerializeField] private RawImage image;

        private Texture2D texture;

        private void Update()
        {
            if (!texture || texture.width != recorder.Buffer.Length)
            {
                Destroy(texture);

                texture = new Texture2D(recorder.Buffer.Length, 100, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None);
                texture.filterMode = FilterMode.Point;

                image.texture = texture;
            }

            var pixels = texture.GetPixels();
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }

            for (var pixelX = 0; pixelX < recorder.Buffer.Length; pixelX++)
            {
                var y = recorder.Buffer[pixelX];
                var pixelY = (int)(texture.height * ((y + 1) / 2));

                var index = pixelY * texture.width + pixelX;
                pixels[index] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply();
        }
    }
}
