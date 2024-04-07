using TMPro;
using UnityEngine;

namespace Source.Networking
{
    public class PlayerCharacter : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private TMP_Text nameText;

        public Player Player { get; set; }

        private void Update()
        {
            nameText.text = Player.Name;
        }
    }
}
