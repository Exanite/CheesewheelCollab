using System;
using UnityEngine;
using csmatio.types;
using csmatio.io;

namespace Source.Audio
{
    public class AudioPlayer : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private AudioRecorder recorder;

        // See https://trello.com/c/rQ9w7TyA/26-audio-format
        [Header("Settings")]
        [SerializeField] private int minChunksBuffered = 5;
        [SerializeField] private int maxChunksBuffered = 10;
        [SerializeField] private int minChunksQueued = 2;

        private float[][] buffers;
        private float[] activeBuffer;

        /// <summary>
        /// Max chunk received from AudioRecorder / network
        /// </summary>
        private int maxReceivedChunk;

        /// <summary>
        /// Last chunk output to speakers
        /// </summary>
        private int lastOutputChunk;

        private AudioOutput output;

        private void Start()
        {
            LoadHRTF();

            activeBuffer = new float[AudioConstants.SamplesChunkSize * 2];
            buffers = new float[256][];
            for (var i = 0; i < buffers.Length; i++)
            {
                buffers[i] = new float[AudioConstants.SamplesChunkSize];
            }

            recorder.SamplesAvailable += OnSamplesAvailable;

            output = new AudioOutput(AudioConstants.SampleRate, 2);
        }

        private void OnDestroy()
        {
            output.Dispose();
        }

        // private int sineChunk;

        private void Update()
        {
            var queuedChunks = output.QueuedSamplesPerChannel / AudioConstants.SamplesChunkSize;
            if (queuedChunks < minChunksQueued)
            {
                if (maxReceivedChunk - lastOutputChunk > maxChunksBuffered)
                {
                    lastOutputChunk = maxReceivedChunk - maxChunksBuffered;
                }

                if (maxReceivedChunk - lastOutputChunk > minChunksBuffered)
                {
                    lastOutputChunk++;
                    
                    //apply HRTF to audio chunk
                    float[] appliedChunk = ApplyHRTF(buffers);
                    appliedChunk.AsSpan().CopyTo(activeBuffer);
                }

                // // Sine wave output (sounds like an organ)
                // for (var i = 0; i < activeBuffer.Length; i++)
                // {
                //     var time = (float)(sineChunk * activeBuffer.Length + i) / AudioConstants.SampleRate;
                //
                //     activeBuffer[i] += 0.025f * Mathf.Sin(Mathf.Sin(2 * Mathf.PI * 220 * time) + 2 * Mathf.PI * 220 * time);
                //     activeBuffer[i] += 0.025f * Mathf.Sin(Mathf.Sin(2 * Mathf.PI * 440 * time) + 2 * Mathf.PI * 440 * time);
                //     activeBuffer[i] += 0.025f * Mathf.Sin(Mathf.Sin(2 * Mathf.PI * 880 * time) + 2 * Mathf.PI * 880 * time);
                //     activeBuffer[i] += 0.025f * Mathf.Sin(Mathf.Sin(2 * Mathf.PI * 1760 * time) + 2 * Mathf.PI * 1760 * time);
                // }
                // sineChunk++;

                // Don't modify code below when processing audio
                for (var i = 0; i < activeBuffer.Length; i++)
                {
                    activeBuffer[i] = Mathf.Clamp(activeBuffer[i], -1, 1);
                }
                output.QueueSamples(activeBuffer);
                activeBuffer.AsSpan().Clear();
            }
        }

        private void OnSamplesAvailable(int chunk, float[] samples)
        {
            // Assumes chunk is strictly increasing
            maxReceivedChunk = Mathf.Max(maxReceivedChunk, chunk);
            samples.CopyTo(buffers[chunk % buffers.Length], 0);
        }

        private void LoadHRTF()
        {
            string path = Application.streamingAssetsPath + "/HRTFs/hrir58.mat";

            MatFileReader mfr = new MatFileReader(path);

            Debug.Log(mfr.MatFileHeader.ToString());
            foreach (MLArray mla in mfr.Data)
            {
                //Debug.Log(mla.ContentToString() + "\n");
            }

            Debug.Log(mfr.Data[0].ContentToString() + "\n"); // OnR
            Debug.Log(mfr.Data[1].ContentToString() + "\n"); // OnL
            Debug.Log(mfr.Data[2].ContentToString() + "\n"); // ITD
            Debug.Log(mfr.Data[3].ContentToString() + "\n"); // hrir_r
            Debug.Log(mfr.Data[4].ContentToString() + "\n"); // hrir_l
            Debug.Log(mfr.Data[5].ContentToString() + "\n"); // subject name
            double[][] mld = ((MLDouble)mfr.Data[2]).GetArray();
            Debug.Log(mld[12][0]);

            
        }

        // placeholder function for how to apply hrtf to streaming audio
        private float[] ApplyHRTF(float[][] buffers)
		{
            float[] currBuffer = buffers[lastOutputChunk % buffers.Length];

            float[] leftChannel = new float[AudioConstants.SamplesChunkSize];
            float[] rightChannel = new float[AudioConstants.SamplesChunkSize];

            currBuffer.AsSpan().CopyTo(leftChannel);
            currBuffer.AsSpan().CopyTo(rightChannel);

            // get direction vector for sound

            // convert to azimuth and elevation angles
            // HRTF measured at 25 azimuth points (1st dim), 50 elevation points (2nd dim),
            // all at 5 degrees offset from the next point
            // azimuth index [0,12] is left side, 13 is middle, [14,25] is right side
            // elevation index 8 is horizontal

            // get correct hrtf for that azimuth and elevation

            // convolve left and right channels against hrir_r, hrir_l

            // delay left or right channel according to ITD
            /* 
            delay = ITD = mfr.Data[2][azimuth][elevation];
            if (aIndex < 13) %sound is on left so delay right
                add floor(delay) frames to end of wav_left 
                add floor(delay) frames to start of wav_right
            else
                add floor(delay) frames to start of wav_left 
                add floor(delay) frames to end of wav_right
            end
             */

            // zip left and right channels together and output
            float[] output = new float[leftChannel.Length * 2];
            for (int i = 0; i < leftChannel.Length; i++)
			{
                output[i*2] = leftChannel[i];
                output[i*2+1] = rightChannel[i];
            }

            return output;

        }
    }
}
