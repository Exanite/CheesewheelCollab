using UnityEngine;

namespace Source.Players
{
    public class LocalPlayerSettings : MonoBehaviour
    {
        [SerializeField] private string playerName;

        public string PlayerName
        {
            get => playerName;
            set => playerName = value;
        }

        private void Awake()
        {
            playerName = $"User {Random.Range(1, 100 + 1)}";
        }
    }
}
