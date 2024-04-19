using Exanite.Core.Utilities;
using TMPro;
using UnityEngine;

namespace Source.Networking
{
    public class PlayerCharacter : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Transform audioVisual;

        [Header("Settings")]
        [SerializeField] private AnimationCurve audioVisualCurve;
        [SerializeField] private float audioVisualMinAmplitude = 0.1f;
        [SerializeField] private float audioVisualMaxAmplitude = 0.3f;
        [SerializeField] private float audioVisualMinScale = 1;
        [SerializeField] private float audioVisualMaxScale = 2;

        public Player Player { get; set; }

        private void Update()
        {
            nameText.text = Player.Name;

            var renderAudioVisual = Player.Audio.AverageAmplitude >= audioVisualMinAmplitude;
            audioVisual.gameObject.SetActive(renderAudioVisual);
            if (renderAudioVisual)
            {
                var scale = MathUtility.Remap(Player.Audio.AverageAmplitude, audioVisualMinAmplitude, audioVisualMaxAmplitude, 0, 1);
                scale = audioVisualCurve.Evaluate(scale);
                scale = MathUtility.Remap(scale, 0, 1, audioVisualMinScale, audioVisualMaxScale);

                audioVisual.localScale = new Vector3(scale, scale, 1);
            }
        }
    }
}
