using System;
using System.Collections.Generic;
using BH.SDK.Models.Values;
using BH.SDK.Rules;

namespace BH.SDK.Utils
{
    /// <summary>
    /// A shape from one outline: the outline fitted to the authored box and cut into triangles, so a tool can
    /// define a CompositeShape from points alone.
    /// </summary>
    public static class ShapePolygonUtils
    {
        /// <summary> The most corners one outline may have: a simple polygon of n corners is n - 2
        /// triangles. </summary>
        public const int MaxOutlinePoints = ValueRules.MaxShapeTriangles + 2;

        /// <summary> Fits <paramref name="outline"/> into the authored box [-0.5, 0.5] on each axis, so a
        /// shape drawn at size <paramref name="width"/> x <paramref name="height"/> around
        /// <paramref name="centerX"/>, <paramref name="centerY"/> shows the outline as given. </summary>
        public static List<Vector2Value> FitToBox(IReadOnlyList<Vector2Value> outline, out float centerX,
            out float centerY, out float width, out float height)
        {
            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var point in outline)
            {
                minX = Math.Min(minX, point.X);
                minY = Math.Min(minY, point.Y);
                maxX = Math.Max(maxX, point.X);
                maxY = Math.Max(maxY, point.Y);
            }

            width = maxX - minX;
            height = maxY - minY;
            centerX = (minX + maxX) * 0.5f;
            centerY = (minY + maxY) * 0.5f;

            var fitted = new List<Vector2Value>(outline.Count);
            foreach (var point in outline)
                fitted.Add(new Vector2Value(
                    width > 0f ? Clamp((point.X - centerX) / width) : 0f,
                    height > 0f ? Clamp((point.Y - centerY) / height) : 0f));
            return fitted;
        }

        /// <summary> Cuts a simple outline (either winding, convex or not) into triangles by ear clipping,
        /// every triangle wound front-facing. False when the outline crosses itself or has no area.
        /// </summary>
        public static bool TryTriangulate(IReadOnlyList<Vector2Value> outline, out List<int> indices)
        {
            indices = new List<int>();
            var count = outline.Count;
            if (count < 3) return false;

            var area = 0.0;
            for (var i = 0; i < count; i++)
            {
                var a = outline[i];
                var b = outline[(i + 1) % count];
                area += (double)a.X * b.Y - (double)b.X * a.Y;
            }

            if (Math.Abs(area) < ShapeGeometryUtils.DegenerateEpsilon) return false;

            var ring = new List<int>(count);
            for (var i = 0; i < count; i++) ring.Add(area > 0 ? i : count - 1 - i);

            while (ring.Count > 3)
            {
                var clipped = false;
                for (var k = 0; k < ring.Count && !clipped; k++)
                {
                    var prev = ring[(k + ring.Count - 1) % ring.Count];
                    var cur = ring[k];
                    var next = ring[(k + 1) % ring.Count];
                    if (!IsEar(outline, ring, prev, cur, next)) continue;

                    indices.Add(prev);
                    indices.Add(cur);
                    indices.Add(next);
                    ring.RemoveAt(k);
                    clipped = true;
                }

                if (!clipped)
                {
                    indices.Clear();
                    return false;
                }
            }

            if (ShapeGeometryUtils.SignedDoubleArea(outline[ring[0]], outline[ring[1]], outline[ring[2]]) <= 0f)
            {
                indices.Clear();
                return false;
            }

            indices.Add(ring[0]);
            indices.Add(ring[1]);
            indices.Add(ring[2]);
            return true;
        }

        private static bool IsEar(IReadOnlyList<Vector2Value> outline, List<int> ring, int prev, int cur, int next)
        {
            var a = outline[prev];
            var b = outline[cur];
            var c = outline[next];
            if (ShapeGeometryUtils.SignedDoubleArea(a, b, c) <= ShapeGeometryUtils.DegenerateEpsilon) return false;

            foreach (var other in ring)
            {
                if (other == prev || other == cur || other == next) continue;
                if (Contains(a, b, c, outline[other])) return false;
            }

            return true;
        }

        // Inclusive of the edges: a corner touching the ear would leave a sliver crossing the outline.
        private static bool Contains(Vector2Value a, Vector2Value b, Vector2Value c, Vector2Value p)
            => ShapeGeometryUtils.SignedDoubleArea(a, b, p) >= 0f
               && ShapeGeometryUtils.SignedDoubleArea(b, c, p) >= 0f
               && ShapeGeometryUtils.SignedDoubleArea(c, a, p) >= 0f;

        private static float Clamp(float value)
            => Math.Max(ValueRules.MinShapePoint, Math.Min(ValueRules.MaxShapePoint, value));
    }
}
