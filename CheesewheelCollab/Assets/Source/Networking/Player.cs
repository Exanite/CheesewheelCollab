using Source.Audio;
using UnityEngine;

namespace Source.Networking
{
    public class Player
    {
        public int Id;
        public string Name = "Player";

        public Vector2 Position;

        /// <summary>
        /// Invalid on server.
        /// </summary>
        public float Volume = 1;

        /// <summary>
        /// Invalid on server.
        /// </summary>
        public PlayerCharacter Character;

        /// <summary>
        /// Invalid on server.
        /// </summary>
        public PlayerAudioData Audio;

        public class PlayerAudioData
        {
            /// <summary>
            /// Circular buffer of audio samples.
            /// </summary>
            public float[][] Buffers;

            /// <summary>
            /// Max chunk received from network.
            /// </summary>
            public int MaxReceivedChunk;

            /// <summary>
            /// Last chunk output to speakers.
            /// </summary>
            public int LastOutputChunk;

            /// <summary>
            /// Exponential weighted moving average of the amplitude.
            /// </summary>
            public float AverageAmplitude;

            public PlayerAudioData()
            {
                Buffers = new float[256][];
                for (var i = 0; i < Buffers.Length; i++)
                {
                    Buffers[i] = new float[AudioConstants.SamplesChunkSize];
                }
            }
        }
    }
}
