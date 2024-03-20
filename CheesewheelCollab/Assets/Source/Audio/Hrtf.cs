using System;
using csmatio.io;
using csmatio.types;
using UnityEngine;

namespace Source.Audio
{
    public class Hrtf
    {
        public const int AzimuthCount = 25;
        public const int ElevationCount = 50;

        public const int ForwardAzimuth = 12;
        public const int HorizontalElevation = 8;

        public const int HrtfSampleCount = 200;

        private double[][] itds;
        private float[][][] leftHrtfs;
        private float[][][] rightHrtfs;

        public Hrtf(MatFileReader reader)
        {
            itds = ((MLDouble)reader.Content["ITD"]).GetArray();

            var rawLeftHrtfs = ((MLDouble)reader.Content["hrir_l"]).GetArray();
            var rawRightHrtfs = ((MLDouble)reader.Content["hrir_r"]).GetArray();

            leftHrtfs = new float[AzimuthCount][][];
            for (var azimuthI = 0; azimuthI < AzimuthCount; azimuthI++)
            {
                leftHrtfs[azimuthI] = new float[ElevationCount][];
                for (var elevationI = 0; elevationI < ElevationCount; elevationI++)
                {
                    var samplesFloats = new float[HrtfSampleCount];
                    var samplesDoubles = rawLeftHrtfs[azimuthI].AsSpan(elevationI * HrtfSampleCount, HrtfSampleCount);
                    for (var i = 0; i < HrtfSampleCount; i++)
                    {
                        samplesFloats[i] = (float)samplesDoubles[i];
                    }

                    leftHrtfs[azimuthI][elevationI] = samplesFloats;
                }
            }
            
            rightHrtfs = new float[AzimuthCount][][];
            for (var azimuthI = 0; azimuthI < AzimuthCount; azimuthI++)
            {
                rightHrtfs[azimuthI] = new float[ElevationCount][];
                for (var elevationI = 0; elevationI < ElevationCount; elevationI++)
                {
                    var samplesFloats = new float[HrtfSampleCount];
                    var samplesDoubles = rawRightHrtfs[azimuthI].AsSpan(elevationI * HrtfSampleCount, HrtfSampleCount);
                    for (var i = 0; i < HrtfSampleCount; i++)
                    {
                        samplesFloats[i] = (float)samplesDoubles[i];
                    }

                    rightHrtfs[azimuthI][elevationI] = samplesFloats;
                }
            }
        }

        public bool IsRight(int azimuth)
        {
            return azimuth > ForwardAzimuth;
        }

        /// <param name="directionToSound">
        /// The local direction to the sound using Unity conventions.
        /// <para/>
        /// Eg: <see cref="Vector3.forward">Vector3.forward</see> corresponds to the forward direction.
        /// </param>
        public int GetItd(Vector3 directionToSound)
        {
            return GetItd(GetAzimuth(directionToSound), GetElevation(directionToSound));
        }

        public int GetItd(int azimuth, int elevation)
        {
            return (int)itds[azimuth][elevation];
        }


        public float[] GetHrtf(Vector3 directionToSound, bool isRight)
        {
            return GetHrtf(GetAzimuth(directionToSound), GetElevation(directionToSound), isRight);
        }

        public float[] GetHrtf(int azimuth, int elevation, bool isRight)
        {
            var hrtfs = isRight ? rightHrtfs : leftHrtfs;
            return hrtfs[azimuth][elevation];
        }

        /// <param name="directionToSound">
        /// The local direction to the sound using Unity conventions.
        /// <para/>
        /// Eg: <see cref="Vector3.forward">Vector3.forward</see> corresponds to the forward direction.
        /// </param>
        public int GetAzimuth(Vector3 directionToSound)
        {
            Vector2 planarDirection = new Vector2(directionToSound.normalized.x, directionToSound.normalized.z);
            double degreesRotated = Math.Round(Math.Acos((double)Vector2.Dot(planarDirection, Vector2.up)) * 180 / Math.PI);
            if (Vector3.Cross(planarDirection, Vector3.forward).y >= 0)
            {
                degreesRotated *= -1;
            }

            Debug.Log("get azimuth degrees: " + degreesRotated);

            // [ -80 -65 -55 -45:5:45 55 65 80 ]
            if (Math.Abs(degreesRotated) > 90)
			{
                if (degreesRotated > 0)
                    degreesRotated = 180 - degreesRotated;
                else
				{
                    degreesRotated = -180 - degreesRotated;
                }
			}

            if (degreesRotated < -72.5) return 0;
            if (degreesRotated < -60)   return 1;
            if (degreesRotated < -50)   return 2;
            if (degreesRotated < -40)   return 3;

            if (degreesRotated > 72.5)  return 24;
            if (degreesRotated > 60)    return 23;
            if (degreesRotated > 50)    return 22;
            if (degreesRotated > 40)    return 21;

            return (int)Math.Round(degreesRotated / 5.0) + 12;
        }

        /// <param name="directionToSound">
        /// The local direction to the sound using Unity conventions.
        /// <para/>
        /// Eg: <see cref="Vector3.forward">Vector3.forward</see> corresponds to the forward direction.
        /// </param>
        public int GetElevation(Vector3 directionToSound)
        {
            throw new NotImplementedException();
        }

        private float[] convolveResult = new float[AudioConstants.SamplesChunkSize];

        /// <remarks>
        /// This function was taken from the Accord Framework, under the LGPL License.
        /// https://github.com/accord-net/framework/blob/1ab0cc0ba55bcc3d46f20e7bbe7224b58cd01854/Sources/Accord.Math/Matrix/Matrix.Common.cs#L1937
        /// </remarks>
        public float[] Convolve(float[] previous, float[] current, float[] next, float[] hrtf)
        {
            var m = (int)Math.Ceiling(hrtf.Length / 2.0);
            for (var i = 0; i < convolveResult.Length; i++)
            {
                convolveResult[i] = 0;
                for (var j = 0; j < hrtf.Length; j++)
                {
                    var k = i - j + m - 1;

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
    }
}
