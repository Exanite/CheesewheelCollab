using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Source.Audio
{
    public abstract class AudioProvider : MonoBehaviour
    {
        private ObjectPool<QueuedChunk> pool = new(() => new QueuedChunk());
        private Queue<QueuedChunk> queuedChunks = new();

        protected readonly float[] Buffer = new float[AudioConstants.SamplesChunkSize];

        /// <summary>
        /// Will be called on the main thread.
        /// </summary>
        public event SamplesAvailableCallback SamplesAvailable;

        protected virtual void Update()
        {
            lock (pool)
            lock (queuedChunks)
            {
                while (queuedChunks.TryDequeue(out var queuedChunk))
                {
                    SamplesAvailable?.Invoke(queuedChunk.Chunk, queuedChunk.Samples);
                    pool.Release(queuedChunk);
                }
            }
        }

        /// <summary>
        /// This can be called on any thread.
        /// </summary>
        protected void OnSamplesAvailable(int chunk, float[] buffer)
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = Mathf.Clamp(buffer[i], -1, 1);
            }

            lock (pool)
            lock (queuedChunks)
            {
                var queuedChunk = pool.Get();
                queuedChunk.Chunk = chunk;
                buffer.AsSpan().CopyTo(queuedChunk.Samples);

                queuedChunks.Enqueue(queuedChunk);
            }
        }

        private class QueuedChunk
        {
            public int Chunk;
            public float[] Samples = new float[AudioConstants.SamplesChunkSize];
        }
    }
}
