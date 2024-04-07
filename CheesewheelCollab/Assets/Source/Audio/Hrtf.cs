using System;
using csmatio.io;
using csmatio.types;
using Exanite.Core.Utilities;
using UnityEngine;

namespace Source.Audio
{
    public class Hrtf
    {
        public const int AzimuthCount = 25;
        public const int ElevationCount = 50;

        public const int ForwardAzimuth = 12;
        public const int HorizontalElevation = 8;

        public const int HrirSampleCount = 200;

        private double[][] itds;
        private float[][][] leftHrirs;
        private float[][][] rightHrirs;

        public Hrtf(MatFileReader reader)
        {
            itds = ((MLDouble)reader.Content["ITD"]).GetArray();

            var rawLeftHrirs = ((MLDouble)reader.Content["hrir_l"]).GetArray();
            var rawRightHrirs = ((MLDouble)reader.Content["hrir_r"]).GetArray();

            leftHrirs = new float[AzimuthCount][][];
            for (var azimuthI = 0; azimuthI < AzimuthCount; azimuthI++)
            {
                leftHrirs[azimuthI] = new float[ElevationCount][];
                for (var elevationI = 0; elevationI < ElevationCount; elevationI++)
                {
                    var samplesFloats = new float[HrirSampleCount];
                    for (var i = 0; i < HrirSampleCount; i++)
                    {
                        samplesFloats[i] = (float)rawLeftHrirs[azimuthI][i * ElevationCount + elevationI];
                    }

                    leftHrirs[azimuthI][elevationI] = samplesFloats;
                }
            }

            rightHrirs = new float[AzimuthCount][][];
            for (var azimuthI = 0; azimuthI < AzimuthCount; azimuthI++)
            {
                rightHrirs[azimuthI] = new float[ElevationCount][];
                for (var elevationI = 0; elevationI < ElevationCount; elevationI++)
                {
                    var samplesFloats = new float[HrirSampleCount];
                    for (var i = 0; i < HrirSampleCount; i++)
                    {
                        samplesFloats[i] = (float)rawRightHrirs[azimuthI][i * ElevationCount + elevationI];
                    }

                    rightHrirs[azimuthI][elevationI] = samplesFloats;
                }
            }
        }

        public bool IsRight(int azimuth)
        {
            return azimuth > ForwardAzimuth;
        }

        public int GetItd(int azimuth, int elevation)
        {
            return (int)itds[azimuth][elevation];
        }

        public float[] GetHrir(int azimuth, int elevation, bool isRight)
        {
            var hrirs = isRight ? rightHrirs : leftHrirs;
            return hrirs[azimuth][elevation];
        }

        /// <param name="directionToSound">
        /// The local direction to the sound using Unity conventions.
        /// <para/>
        /// Eg: <see cref="Vector3.forward">Vector3.forward</see> corresponds to the forward direction.
        /// </param>
        public int GetAzimuth(Vector3 directionToSound)
        {
            var planarDirection = new Vector2(directionToSound.normalized.x, directionToSound.normalized.z);
            var degreesRotated = Mathf.Round(Mathf.Acos(Vector2.Dot(planarDirection, Vector2.up)) * 180f / Mathf.PI);
            if (Vector3.Cross(planarDirection, Vector3.forward).y >= 0)
            {
                degreesRotated *= -1;
            }

            // [ -80 -65 -55 -45:5:45 55 65 80 ]
            if (Mathf.Abs(degreesRotated) > 90)
            {
                if (degreesRotated > 0)
                {
                    degreesRotated = 180 - degreesRotated;
                }
                else
                {
                    degreesRotated = -180 - degreesRotated;
                }
            }

            if (degreesRotated < -72.5f) return 0;
            if (degreesRotated < -60)   return 1;
            if (degreesRotated < -50)   return 2;
            if (degreesRotated < -40)   return 3;

            if (degreesRotated > 72.5f)  return 24;
            if (degreesRotated > 60)    return 23;
            if (degreesRotated > 50)    return 22;
            if (degreesRotated > 40)    return 21;

            return Mathf.RoundToInt(degreesRotated / 5) + 12;
        }

        /// <param name="directionToSound">
        /// The local direction to the sound using Unity conventions.
        /// <para/>
        /// Eg: <see cref="Vector3.forward">Vector3.forward</see> corresponds to the forward direction.
        /// </param>
        public int GetElevation(Vector3 directionToSound)
        {
            var planarDirection = new Vector2(directionToSound.normalized.x, directionToSound.normalized.z);
            double degreesRotated = Mathf.Round(Mathf.Acos(Vector2.Dot(planarDirection, Vector2.up)) * 180 / Mathf.PI);
            if (degreesRotated > 90)
            {
                return 40; // Behind you
            }
            else
            {
                return 8; // Ahead of you
            }
        }

        private float[] convolveResult = new float[AudioConstants.SamplesChunkSize];

        /// <remarks>
        /// This function was taken from the Accord Framework, under the LGPL License.
        /// https://github.com/accord-net/framework/blob/1ab0cc0ba55bcc3d46f20e7bbe7224b58cd01854/Sources/Accord.Math/Matrix/Matrix.Common.cs#L1937
        /// </remarks>
        public float[] Convolve(float[] previous, float[] current, float[] next, float[] hrtf, int offset = 0)
        {
            var m = Mathf.CeilToInt(hrtf.Length / 2f);
            for (var i = 0; i < convolveResult.Length; i++)
            {
                convolveResult[i] = 0;
                for (var j = 0; j < hrtf.Length; j++)
                {
                    var k = i - j + m - 1 + offset;

                    if (k < 0)
                    {
                        convolveResult[i] += previous[k + AudioConstants.SamplesChunkSize] * hrtf[j];
                    }
                    else if (k >= AudioConstants.SamplesChunkSize)
                    {
                        convolveResult[i] += next[k - AudioConstants.SamplesChunkSize] * hrtf[j];
                    }
                    else
                    {
                        convolveResult[i] += current[k] * hrtf[j];
                    }
                }
            }

            return convolveResult;
        }

        public float[] Apply(ApplyHrtfOptions options)
        {
            // --- Get variables and buffers ---
            var offsetToSound = options.OffsetToSound;
            var attenuationCurve = options.AttenuationCurve;
            var attenuationStart = options.AttenuationStart;
            var attenuationEnd = options.AttenuationEnd;

            var previousChunk = options.PreviousChunk;
            var currentChunk = options.CurrentChunk;
            var nextChunk = options.NextChunk;
            var leftChannel = options.LeftChannel;
            var rightChannel = options.RightChannel;
            var resultsBuffer = options.ResultsBuffer;

            // --- Calculate attenuation ---
            var distance = offsetToSound.magnitude;
            var attenuation = attenuationCurve.Evaluate(MathUtility.Remap(Mathf.Clamp(distance, attenuationStart, attenuationEnd), attenuationStart, attenuationEnd, 1, 0));

            if (attenuation < 0.001f)
            {
                resultsBuffer.AsSpan().Clear();

                return resultsBuffer;
            }

            // --- Calculate direction ---
            var azimuth = GetAzimuth(offsetToSound);
            var elevation = GetElevation(offsetToSound);

            // --- Calculate ITD ---
            var delayInSamples = GetItd(azimuth, elevation);
            var leftDelay = IsRight(azimuth) ? -delayInSamples : 0;
            var rightDelay = IsRight(azimuth) ? 0 : -delayInSamples;

            // --- Apply HRIR and ITD ---
            var leftHrir = GetHrir(azimuth, elevation, false);
            var rightHrir = GetHrir(azimuth, elevation, true);

            Convolve(previousChunk, currentChunk, nextChunk, leftHrir, leftDelay).AsSpan().CopyTo(leftChannel);
            Convolve(previousChunk, currentChunk, nextChunk, rightHrir, rightDelay).AsSpan().CopyTo(rightChannel);

            // ! This is disabled because it causes popping and not actually needed
            // // --- Normalize volume ---
            // // Calculate original max amplitude
            // var originalMaxAmplitude = 0f;
            // for (var i = 0; i < AudioConstants.SamplesChunkSize; i++)
            // {
            //     originalMaxAmplitude = Mathf.Max(originalMaxAmplitude, Mathf.Abs(currentChunk[i]));
            // }
            //
            // // Calculate max amplitude after convolution
            // var convolvedMaxAmplitude = 0f;
            // for (var i = 0; i < AudioConstants.SamplesChunkSize; i++)
            // {
            //     convolvedMaxAmplitude = Mathf.Max(convolvedMaxAmplitude, Mathf.Max(Mathf.Abs(leftChannel[i]), Mathf.Abs(rightChannel[i])));
            // }
            //
            // // Reduce to original amplitude
            // var amplitudeFactor = convolvedMaxAmplitude / originalMaxAmplitude;
            // if (originalMaxAmplitude > 1)
            // {
            //     // Reduce max amplitude to 1
            //     amplitudeFactor *= originalMaxAmplitude;
            // }
            //
            // if (amplitudeFactor > 1)
            // {
            //     for (var i = 0; i < AudioConstants.SamplesChunkSize; i++)
            //     {
            //         leftChannel[i] /= amplitudeFactor;
            //         rightChannel[i] /= amplitudeFactor;
            //     }
            // }

            // --- Copy to output and apply attenuation ---
            // Cannot change output size, otherwise we record and consume at different rates
            for (var i = 0; i < AudioConstants.SamplesChunkSize; i++)
            {
                // Zip left and right channels together
                resultsBuffer[i * 2] = leftChannel[i] * attenuation;
                resultsBuffer[i * 2 + 1] = rightChannel[i] * attenuation;
            }

            return resultsBuffer;
        }
    }
}
