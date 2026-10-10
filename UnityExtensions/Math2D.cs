using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace BH.SDK
{
    // The 2D rotation primitives live here because Shared references the SDK, never the reverse. They are
    // NON-extension statics: a second RotateVector(this float2, ...) would be ambiguous beside BHMath's.

    /// <summary> Rotation of 2D vectors and angles, shared by the SDK's Unity-facing types. </summary>
    public static class Math2D
    {
        /// <summary> A quarter turn, in radians. </summary>
        public const float HalfPI = math.PI / 2f;

        /// <summary> <paramref name="vector"/> turned counter-clockwise by <paramref name="radians"/>. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 RotateVector(float2 vector, float radians)
        {
            math.sincos(radians, out var sin, out var cos);
            return RotateVectorFast(vector, sin, cos);
        }

        /// <summary> The same turn against a sine and cosine the caller already has - what a loop
        /// rotating many vectors by one angle should use. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 RotateVectorFast(float2 vector, float sin, float cos)
        {
            return new float2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos
            );
        }

        /// <summary> The unit vector pointing at <paramref name="radians"/>. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 GetVector(float radians)
        {
            math.sincos(radians, out var sin, out var cos);
            return GetVectorFast(sin, cos);
        }

        /// <summary> The same vector against a sine and cosine the caller already has. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 GetVectorFast(float sin, float cos)
        {
            return new float2(cos, sin);
        }

        /// <summary> A rotation of <paramref name="angle"/> radians about Z - the only axis a 2D
        /// transform turns about, built directly rather than through Quaternion.Euler. </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion RotateZ(float angle)
        {
            math.sincos(0.5f * angle, out var sinA, out var cosA);
            return new Quaternion(0.0f, 0.0f, sinA, cosA);
        }
    }
}
