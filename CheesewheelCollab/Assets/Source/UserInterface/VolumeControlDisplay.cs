using UnityEngine;
using Source.Networking;
using TMPro;
using UniDi;
using UnityEngine.UI;

namespace Source.UserInterface
{
    public class VolumeControlDisplay : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private TMP_Text labelText;
        [SerializeField] private Slider volumeSlider;

        [Inject] private NetworkGameManager gameManager;

        private Player player;

        public Player Player
        {
            get => player;
            set
            {
                player = value;

                if (player != null)
                {
                    volumeSlider.value = player.Volume;
                }
            }
        }

        private void Start()
        {
            volumeSlider.onValueChanged.AddListener(value =>
            {
                if (Player == null)
                {
                    return;
                }

                Player.Volume = value;
            });
        }

        private void Update()
        {
            if (Player == null)
            {
                return;
            }

            var isLocal = Player == gameManager.ClientData.LocalPlayer;
            if (isLocal)
            {
                return;
            }

            labelText.text = Player.Name;
        }
    }
}
