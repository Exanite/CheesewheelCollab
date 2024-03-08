using UnityEngine;

namespace Source.Player
{
    public class LocalPlayerSettings : MonoBehaviour
    {
        [SerializeField] private string playerName;

        public string PlayerName
        {
            get => playerName;
            set => playerName = value;
        }
    }
}
