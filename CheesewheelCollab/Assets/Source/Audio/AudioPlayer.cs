using UnityEngine;
using csmatio.types;
using csmatio.io;

namespace Source.Audio
{
    public class AudioPlayer : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private UnityAudioRecorder recorder;
        [SerializeField] private AudioSource audioSource;

        [Header("Settings")]
        private float delaySeconds = 1;

        private float[][] buffers;
        private AudioClip playbackClip;

        private void Start()
        {
            LoadHRTF();

            playbackClip = AudioClip.Create("Playback", AudioConstants.RecordingSampleRate * 10, 2, AudioConstants.RecordingSampleRate, false);
            audioSource.clip = playbackClip;
            audioSource.loop = true;
            audioSource.Play();

            var samples = new float[AudioConstants.AudioPacketSamplesSize];
            for (var chunkI = 0; chunkI < playbackClip.samples / AudioConstants.AudioPacketSamplesSize; chunkI++)
            {
                for (var sampleI = 0; sampleI < AudioConstants.AudioPacketSamplesSize; sampleI++)
                {
                    var time = (chunkI * sampleI) / AudioConstants.RecordingSampleRate;
                    var y = Mathf.Sin(2 * Mathf.PI * time * 440);
                    samples[sampleI] = y;
                }

                var startIndex = chunkI * AudioConstants.AudioPacketSamplesSize;
                playbackClip.SetData(samples, startIndex);
            }

            buffers = new float[256][];
            for (var i = 0; i < buffers.Length; i++)
            {
                buffers[i] = new float[AudioConstants.AudioPacketSamplesSize];
            }

            recorder.SamplesRecorded += OnSamplesRecorded;
        }

        private void OnSamplesRecorded(int sequence, float[] samples)
        {
            samples.CopyTo(buffers[sequence % buffers.Length], 0);
        }

        private void LoadHRTF()
        {
            //HRTF measured at 25 azimuth points (1st dim), 50 elevation points (2nd dim),
            //  all at 5 degrees offset from the next point
            string path = Application.dataPath + "/Content/HRTFs/hrir58.mat";

            MatFileReader mfr = new MatFileReader(path);

            Debug.Log(mfr.MatFileHeader.ToString());
            foreach (MLArray mla in mfr.Data)
            {
                //Debug.Log(mla.ContentToString() + "\n");
            }

            Debug.Log(mfr.Data[1].ContentToString() + "\n");
            double[][] mld = ((MLDouble)mfr.Data[1]).GetArray();
            Debug.Log(mld[0][0]);

        }
    }
}
