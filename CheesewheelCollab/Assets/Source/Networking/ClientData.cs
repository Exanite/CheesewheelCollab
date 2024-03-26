using Source.Audio;

namespace Source.Networking
{
    public class ClientData
    {
        public Player LocalPlayer;

        // Processing per player
        public float[] PreviousChunk = new float[AudioConstants.SamplesChunkSize];
        public float[] CurrentChunk = new float[AudioConstants.SamplesChunkSize];
        public float[] NextChunk = new float[AudioConstants.SamplesChunkSize];
        public float[] LeftChannel = new float[AudioConstants.SamplesChunkSize];
        public float[] RightChannel = new float[AudioConstants.SamplesChunkSize];
        public float[] ResultsBuffer = new float[AudioConstants.SamplesChunkSize * 2];

        // Contains combined audio signals from all players
        public float[] OutputBuffer = new float[AudioConstants.SamplesChunkSize * 2];
        public AudioOutput Output = new AudioOutput(AudioConstants.SampleRate, 2);

        public Hrtf Hrtf;
        public HrtfSubject LoadedSubject;
    }
}
