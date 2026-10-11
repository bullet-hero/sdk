using Unity.Mathematics;

namespace BH.SDK.Transforms
{
    /// <summary> Inverses of <see cref="RectTransform2D.Apply"/>: from a world point back to the local values
    /// a child would have to hold to land there. </summary>
    public static class RectTransform2DMath
    {
        // Lockstep: RectTransform2D.Apply. A root is not composed, so its local is its world point; against
        // RectTransform2D.Zero (no size, no turn, unit scale) this returns the point unchanged.

        /// <summary> The anchored position that puts a child's pivot at <paramref name="worldPivotPoint"/>
        /// under a parent whose GLOBAL transform is <paramref name="parentGlobal"/>. False when the parent's
        /// scale has a zero component, which maps every local position to one line. </summary>
        /// <param name="parentGlobal">The parent's composed (global) transform.</param>
        /// <param name="anchorMin">The child's lower anchor.</param>
        /// <param name="anchorMax">The child's upper anchor.</param>
        /// <param name="pivot">The child's pivot.</param>
        /// <param name="worldPivotPoint">Where the child's pivot should be, in world space.</param>
        /// <param name="localPosition">The child's anchored position; zero on failure.</param>
        public static bool WorldToLocal(in RectTransform2D parentGlobal, float2 anchorMin, float2 anchorMax,
            float2 pivot, float2 worldPivotPoint, out float2 localPosition)
        {
            var parentScale = parentGlobal.scale;
            if (parentScale.x == 0f || parentScale.y == 0f)
            {
                localPosition = float2.zero;
                return false;
            }

            var parentHalfFullSize = parentGlobal.HalfFullSize;
            math.sincos(parentGlobal.rotation, out var parentSin, out var parentCos);

            var parentPivotPoint = math.lerp(-parentHalfFullSize, parentHalfFullSize, parentGlobal.pivot);
            parentPivotPoint = Math2D.RotateVectorFast(parentPivotPoint, parentSin, parentCos);
            var parentCenterPoint = parentGlobal.position - parentPivotPoint;

            var anchorNormalized = math.lerp(anchorMin, anchorMax, pivot);
            var anchorLocal = math.lerp(-parentHalfFullSize, parentHalfFullSize, anchorNormalized);

            // Rotating back by -rotation undoes both rotated terms at once; the anchor term was never scaled.
            var unrotated = Math2D.RotateVectorFast(worldPivotPoint - parentCenterPoint, -parentSin, parentCos);
            localPosition = (unrotated - anchorLocal) / parentScale;

            if (math.all(math.isfinite(localPosition))) return true;
            localPosition = float2.zero;
            return false;
        }
    }
}
