using BH.SDK.Utils;
using Unity.Mathematics;

namespace BH.SDK.UnityExtensions.Tests
{
    // Component-wise approximate equality for the vector types, and NOT a test itself - the same kind
    // of shared fixture support MockData.cs and AsyncAssert.cs are in the core test assembly.
    //
    // IT DELEGATES RATHER THAN RESTATES. The consumer's BH.Shared.BHMath has these overloads and these
    // tests used to call them, which they cannot any more: Shared references this assembly, so this
    // assembly may not reference Shared. The scalar comparison still comes from one place - the SDK's
    // own BHSDKMath - so a tolerance change reaches both halves of the project at once.
    //
    // It lives here rather than in production code because nothing shipping needs it: BHSDKMath cannot
    // take a float2 (the core assembly has no Unity.Mathematics) and no type in UnityExtensions
    // compares vectors. Move it up the moment one does.

    /// <summary> Component-wise <see cref="BHSDKMath.Approximately"/> for the vector types. </summary>
    internal static class Approx
    {
        public static bool Equal(float a, float b) => BHSDKMath.Approximately(a, b);

        public static bool Equal(float2 a, float2 b)
            => Equal(a.x, b.x) && Equal(a.y, b.y);

        public static bool Equal(float3 a, float3 b)
            => Equal(a.x, b.x) && Equal(a.y, b.y) && Equal(a.z, b.z);

        public static bool Equal(float4 a, float4 b)
            => Equal(a.x, b.x) && Equal(a.y, b.y) && Equal(a.z, b.z) && Equal(a.w, b.w);
    }
}
