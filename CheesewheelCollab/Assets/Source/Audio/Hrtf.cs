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
        public const int HorizontalAzimuth = 8;

        public const int HrtfSampleCount = 200;

        private double[][] itds;
        private float[][][] leftHrtfs;
        private float[][][] rightHrtfs;

        public Hrtf(MatFileReader reader)
        {
            itds = ((MLDouble)reader.Content["ITD"]).GetArray();

            var rawLeftHrtfs = ((MLDouble)reader.Content["hrir_l"]).GetArray(); // Need to unpack and convert to float arrays
            var rawRightHrtfs = ((MLDouble)reader.Content["hrir_r"]).GetArray(); // Need to unpack and convert to float arrays
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
            return 0; // Todo
        }

        public int GetItd(int azimuth, int elevation)
        {
            return (int)itds[azimuth][elevation];
        }

        /// <param name="directionToSound">
        /// The local direction to the sound using Unity conventions.
        /// <para/>
        /// Eg: <see cref="Vector3.forward">Vector3.forward</see> corresponds to the forward direction.
        /// </param>
        public float[] GetHrtf(Vector3 directionToSound, bool isRight)
        {
            return new float[HrtfSampleCount]; // Todo
        }

        public float[] GetHrtf(int azimuth, int elevation, bool isRight)
        {
            return new float[HrtfSampleCount]; // Todo
        }
    }
}
