using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace BH.SDK
{
    // THE 2D ROTATION PRIMITIVES, HERE RATHER THAN IN THE CONSUMER, and the reason is which way the
    // dependency runs. BH.Shared.BHMath owns the project's whole math helper library and cannot be
    // referenced from this repository - Shared references the SDK, never the reverse - but three types
    // that now live here (AvatarMovement, Transform2D, RectTransform2D) need exactly these four
    // functions. So the four moved down and BHMath's own overloads delegate to them.
    //
    // THEY ARE NOT EXTENSION METHODS, AND THAT IS THE WHOLE POINT OF THE SHAPE. BHMath already declares
    // `RotateVector(this float2, float)`; a second extension with the same signature in another
    // namespace is ambiguous in every file that has both namespaces in scope, which is most of the
    // project. Plain statics cannot collide, so BHMath keeps its extension form, every existing
    // `.RotateVector(...)` call site keeps compiling, and there is still only one implementation.

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
