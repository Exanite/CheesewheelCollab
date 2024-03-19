namespace Source.Audio
{
    public static class AudioConstants
    {
        public const int SampleRate = 44100;

        /// <summary>
        /// The number of float32 samples in each chunk of samples. This should be used everywhere in the project.
        /// </summary>
        public const int SamplesChunkSize = 500;
    }
}
