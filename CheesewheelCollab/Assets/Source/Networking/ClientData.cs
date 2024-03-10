using Source.Audio;

namespace Source.Networking
{
    public class ClientData
    {
        public Player LocalPlayer;
        public AudioOutput Output = new AudioOutput(AudioConstants.SampleRate, 1);
        public float[] ProcessingBuffer = new float[AudioConstants.SamplesChunkSize];
    }
}
