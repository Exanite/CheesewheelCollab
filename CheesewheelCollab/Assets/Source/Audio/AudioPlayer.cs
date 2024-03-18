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
        private float[] processingBuffer;

        /// <summary>
        /// Max chunk received from AudioRecorder / network
        /// </summary>
        private int maxReceivedChunk;

        /// <summary>
        /// Last chunk output to speakers
        /// </summary>
        private int lastOutputChunk;

        private AudioOutput output;

        private MatFileReader mfr;

        private void Start()
        {
            LoadHRTF();

            processingBuffer = new float[AudioConstants.SamplesChunkSize * 2];
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
                    for (int i = 0; i < processingBuffer.Length; i++)
					{
                        processingBuffer[i] = appliedChunk[i];
					}
                    //appliedChunk.AsSpan().CopyTo(activeBuffer);
                }

                // Don't modify code below when processing audio
                for (var i = 0; i < processingBuffer.Length; i++)
                {
                    processingBuffer[i] = Mathf.Clamp(processingBuffer[i], -1, 1);
                }
                output.QueueSamples(processingBuffer);
                processingBuffer.AsSpan().Clear();
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

            mfr = new MatFileReader(path);

            Debug.Log(mfr.MatFileHeader.ToString());
            foreach (MLArray mla in mfr.Data)
            {
                //Debug.Log(mla.ContentToString() + "\n");
            }

            Debug.Log(mfr.Content["OnR"].ContentToString() + "\n"); // OnR
            Debug.Log(mfr.Content["OnL"].ContentToString() + "\n"); // OnL
            Debug.Log(mfr.Content["ITD"].ContentToString() + "\n"); // ITD
            Debug.Log(mfr.Content["hrir_r"].ContentToString() + "\n"); // hrir_r
            Debug.Log(mfr.Content["hrir_l"].ContentToString() + "\n"); // hrir_l
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
            int aIndex = 13;
            int eIndex = 8;

            // get correct hrtf for that azimuth and elevation
            Debug.Log(((MLDouble)mfr.Content["hrir_l"]).GetArray()[aIndex][eIndex].ToString()); //this would idealy print an array
            double[] hrir_l;


            // convolve left and right channels against hrir_r, hrir_l
            //HRTFProcessing.Convolve(leftChannel, hrir_l);

            // delay left or right channel according to ITD
            int numDelaySamples = (int)((MLDouble)mfr.Content["ITD"]).GetArray()[aIndex][eIndex];
            Debug.Log("numDelaySamples: " + numDelaySamples);
            if (aIndex < 13) //add delay end of left, start of right
			{
                float[] temp = leftChannel;
                leftChannel = new float[temp.Length + numDelaySamples];
                for (int i = 0; i < temp.Length; i++)
				{
                    leftChannel[i] = temp[i];
				}

                temp = rightChannel;
                rightChannel = new float[temp.Length + numDelaySamples];
                for (int i = 0; i < temp.Length; i++)
                {
                    rightChannel[i + numDelaySamples] = temp[i];
                }
            }
            else //add delay start of left, end of right
            {
                float[] temp = leftChannel;
                leftChannel = new float[temp.Length + numDelaySamples];
                for (int i = 0; i < temp.Length; i++)
                {
                    leftChannel[i + numDelaySamples] = temp[i];
                }

                temp = rightChannel;
                rightChannel = new float[temp.Length + numDelaySamples];
                for (int i = 0; i < temp.Length; i++)
                {
                    rightChannel[i] = temp[i];
                }
            }

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
