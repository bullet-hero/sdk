using BH.SDK.Utils;
using Unity.Mathematics;

namespace BH.SDK.UnityExtensions.Tests
{
    // Component-wise approximate equality for vectors; shared fixture support, not a test. Scalars delegate
    // to BHSDKMath so a tolerance change reaches both halves. Not production code: nothing shipping compares
    // vectors.

    /// <summary> Component-wise <see cref="BHSDKMath.Approximately"/> for the vector types. </summary>
    internal static class Approx
    {
        /// <summary> Float comparison with the SDK's own tolerance. </summary>
        public static bool Equal(float a, float b) => BHSDKMath.Approximately(a, b);

        /// <summary> The same, per component. </summary>
        public static bool Equal(float2 a, float2 b)
            => Equal(a.x, b.x) && Equal(a.y, b.y);

        /// <summary> The same, per component. </summary>
        public static bool Equal(float3 a, float3 b)
            => Equal(a.x, b.x) && Equal(a.y, b.y) && Equal(a.z, b.z);

        /// <summary> The same, per component. </summary>
        public static bool Equal(float4 a, float4 b)
            => Equal(a.x, b.x) && Equal(a.y, b.y) && Equal(a.z, b.z) && Equal(a.w, b.w);
    }
}
