using System;

namespace BH.SDK.Utils
{
    // Mixed functions from UnityEngine.Mathf and Unity.Collections.math

    /// <summary> The maths the SDK needs without an engine - the pieces of UnityEngine.Mathf and
    /// Unity.Mathematics that a level model has to be able to use on a server. </summary>
    public static class BHSDKMath
    {
        /// <summary> Pi, at float precision. </summary>
        public const float PI = 3.1415927f;

        /// <summary> A full turn in radians. </summary>
        public const float PI2 = PI * 2f;
        
        /// <summary> The smallest difference a float can represent near one. </summary>
        public const float Epsilon = 1.1920928955078125e-7f;

        /// <summary> The tolerance <see cref="Approximately"/> uses - loose enough to survive a round trip. </summary>
        public const float ApproxEpsilon = Epsilon * 8f;
        
        /// <summary> The value held inside two bounds. </summary>
        public static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            return value <= max ? value : max;
        }
        /// <summary> The value held inside two bounds. </summary>
        public static int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;
            return value <= max ? value : max;
        }

        /// <summary> The value held inside zero and one. </summary>
        public static float Clamp01(float value)
        {
            if (value < 0.0) return 0.0f;
            return value <= 1.0 ? value : 1f;
        }
        
        /// <summary> The smaller of two. </summary>
        public static float Min(float a, float b) => a >= b ? b : a;
        /// <summary> The larger. </summary>
        public static float Max(float a, float b) => a <= b ? b : a;
        /// <summary> Its magnitude. </summary>
        public static float Abs(float f) => Math.Abs(f);
        
        /// <summary> True when two floats are equal to within <see cref="ApproxEpsilon"/>. </summary>
        public static bool Approximately(float a, float b)
        {
            return Abs(b - a) < Max(1E-06f * Max(Abs(a), Abs(b)), ApproxEpsilon);
        }
    }
}