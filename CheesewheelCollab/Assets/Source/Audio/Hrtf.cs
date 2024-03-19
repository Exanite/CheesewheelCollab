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
            throw new NotImplementedException();
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
    }
}
