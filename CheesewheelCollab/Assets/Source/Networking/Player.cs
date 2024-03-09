using UnityEngine;

namespace Source.Networking
{
    public class Player
    {
        public int Id;

        public Vector2 Position;

        /// <summary>
        /// Null on server.
        /// </summary>
        public GameObject GameObject;
    }
}
