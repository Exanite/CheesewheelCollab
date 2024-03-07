namespace Source.Audio
{
    public static class AudioConstants
    {
        /// <summary>
        /// Recording sample rate. This is currently the playback rate, but will likely not be in the future.
        /// </summary>
        public const int SampleRate = 10000;

        // Future:
        // public const int RecordingSampleRate = 10000;
        // public const int HrtfSampleRate = 44100;
        // public const int PlaybackSampleRate = 44100;

        /// <summary>
        /// The number of float32 samples in each chunk of samples. This should be used everywhere in the project.
        /// </summary>
        public const int SamplesChunkSize = 500;
    }
}
