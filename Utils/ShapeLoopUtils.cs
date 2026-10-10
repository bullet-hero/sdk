using System;
using System.Collections.Generic;
using BH.SDK.Models.Values;

namespace BH.SDK.Utils
{
    // Every built-in shape is loops plus a sector: a fan and an annulus emitter cover every combination.
    // Angles run CLOCKWISE FROM STRAIGHT UP (Docs/SHAPES.md). Separate from ShapeSynthUtils.

    /// <summary>
    /// Radial loops, the intermediate form of the built-in shape catalogue: points ordered by clockwise angle
    /// from straight up, star-shaped about the centre they are used with.
    /// </summary>
    public static class ShapeLoopUtils
    {
        /// <summary> Half-extent of the authored box. </summary>
        public const float BoxRadius = 0.5f;

        /// <summary> Circumradius of the box itself, i.e. the radius its four corners sit at. </summary>
        public const float BoxCornerRadius = 0.70710678f;

        /// <summary> A full turn in radians, at double precision. </summary>
        public const double Tau = Math.PI * 2.0;

        // Angles are always RELATIVE: an absolute x = -1e-17 corner sorts to the far end and reverses a
        // sector.
        private const double AngleEpsilon = 1e-6;

        #region Angles

        /// <summary> The point at <paramref name="angle"/> clockwise from straight up. </summary>
        public static Vector2Value OnClock(double angle, float radius)
            => new((float)(Math.Sin(angle) * radius), (float)(Math.Cos(angle) * radius));

        /// <summary> A point's clockwise angle from straight up, in [0, 2pi). </summary>
        public static double ClockAngle(Vector2Value point)
        {
            var angle = Math.Atan2(point.X, point.Y);
            return angle < 0.0 ? angle + Tau : angle;
        }

        /// <summary>
        /// How far round from <paramref name="reference"/> a point sits, seen from
        /// <paramref name="centre"/>, in [0, 2pi) - with anything within a whisker of a full turn
        /// snapped back onto zero.
        /// </summary>
        public static double RelativeAngle(Vector2Value point, Vector2Value centre, double reference)
        {
            var angle = Math.Atan2(point.X - centre.X, point.Y - centre.Y) - reference;
            angle %= Tau;
            if (angle < 0.0) angle += Tau;
            if (angle > Tau - AngleEpsilon) angle -= Tau;
            return Math.Abs(angle) < AngleEpsilon ? 0.0 : angle;
        }

        #endregion

        #region Loops

        /// <summary> Corners of a regular polygon, ascending in angle. With
        /// <paramref name="halfStepPhase"/> off the first corner points straight up. </summary>
        public static List<Vector2Value> RegularRim(int sides, float radius, bool halfStepPhase)
        {
            var step = Tau / sides;
            var phase = halfStepPhase ? step * 0.5 : 0.0;

            var loop = new List<Vector2Value>(sides);
            for (var i = 0; i < sides; i++)
                loop.Add(OnClock(phase + i * step, radius));
            return loop;
        }

        // The radius fits the BOUNDING BOX to the unit box, not every corner within half a unit (an odd side
        // count differs: a circumradius triangle is only 0.433 wide).

        /// <summary> The circumradius at which a regular polygon's bounding box exactly fills the
        /// authored box. </summary>
        public static float FitRadius(int sides, bool halfStepPhase)
        {
            var unit = RegularRim(sides, 1f, halfStepPhase);
            GetBounds(unit, out var min, out var max);

            var extent = Math.Max(max.X - min.X, max.Y - min.Y);
            return extent > 0f ? 1f / extent : BoxRadius;
        }

        /// <summary> The authored box as a loop - what an inverted shape is subtracted from. </summary>
        public static List<Vector2Value> BoxRim() => RegularRim(4, BoxCornerRadius, halfStepPhase: true);

        // Its corners are the box's, minus the top-right one. Not built from a rim because it is the
        // one form that is not regular, and not worth a special case anywhere else: every operation
        // below takes a loop and does not ask where it came from.

        /// <summary> Corners of the right triangle, ascending in angle. </summary>
        public static List<Vector2Value> RightTriangleRim() => new()
        {
            new Vector2Value(BoxRadius, -BoxRadius),
            new Vector2Value(-BoxRadius, -BoxRadius),
            new Vector2Value(-BoxRadius, BoxRadius),
        };

        #endregion

        #region Transforms

        /// <summary> The box a loop occupies, which is what fitting a shape into its rect needs. </summary>
        public static void GetBounds(IReadOnlyList<Vector2Value> loop,
            out Vector2Value min, out Vector2Value max)
        {
            if (loop.Count == 0)
            {
                min = Vector2Value.Zero;
                max = Vector2Value.Zero;
                return;
            }

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            for (var i = 0; i < loop.Count; i++)
            {
                var point = loop[i];
                if (point.X < minX) minX = point.X;
                if (point.Y < minY) minY = point.Y;
                if (point.X > maxX) maxX = point.X;
                if (point.Y > maxY) maxY = point.Y;
            }

            min = new Vector2Value(minX, minY);
            max = new Vector2Value(maxX, maxY);
        }

        /// <summary> Centre of a loop's bounding box - the offset that AABB-centres it, negated. </summary>
        public static Vector2Value GetBoundsCenter(IReadOnlyList<Vector2Value> loop)
        {
            GetBounds(loop, out var min, out var max);
            return new Vector2Value((min.X + max.X) * 0.5f, (min.Y + max.Y) * 0.5f);
        }

        /// <summary> Average of a loop's corners. For a regular polygon and for a triangle alike this
        /// is the centre of mass. </summary>
        public static Vector2Value GetCornerCentroid(IReadOnlyList<Vector2Value> loop)
        {
            if (loop.Count == 0) return Vector2Value.Zero;

            double x = 0.0, y = 0.0;
            for (var i = 0; i < loop.Count; i++)
            {
                x += loop[i].X;
                y += loop[i].Y;
            }
            return new Vector2Value((float)(x / loop.Count), (float)(y / loop.Count));
        }

        /// <summary> A new loop shifted by an offset. </summary>
        public static List<Vector2Value> Translate(IReadOnlyList<Vector2Value> loop, Vector2Value offset)
        {
            var result = new List<Vector2Value>(loop.Count);
            for (var i = 0; i < loop.Count; i++)
                result.Add(new Vector2Value(loop[i].X + offset.X, loop[i].Y + offset.Y));
            return result;
        }

        // Shrink towards the CORNER CENTROID, not the origin: the right triangle's hypotenuse passes through
        // it.

        /// <summary> A loop shrunk towards its own centroid - the inner rim of a ring. </summary>
        public static List<Vector2Value> Inset(IReadOnlyList<Vector2Value> loop, float thickness)
        {
            var centre = GetCornerCentroid(loop);
            var factor = 1f - thickness;

            var result = new List<Vector2Value>(loop.Count);
            for (var i = 0; i < loop.Count; i++)
            {
                var point = loop[i];
                result.Add(new Vector2Value(
                    centre.X + (point.X - centre.X) * factor,
                    centre.Y + (point.Y - centre.Y) * factor));
            }
            return result;
        }

        #endregion

        #region Sectors

        /// <summary>
        /// The part of a closed loop lying between two angles, as an open polyline running from the
        /// first cut ray to the second. The loop must be star-shaped about the origin.
        /// </summary>
        public static List<Vector2Value> ClipToSector(IReadOnlyList<Vector2Value> loop,
            double from, double to)
        {
            var span = to - from;
            var origin = Vector2Value.Zero;
            var result = new List<Vector2Value>(loop.Count + 2);

            if (TryPointAtAngle(loop, from, origin, closed: true, out var start)) result.Add(start);

            for (var i = 0; i < loop.Count; i++)
            {
                var angle = RelativeAngle(loop[i], origin, from);
                if (angle > AngleEpsilon && angle < span - AngleEpsilon) result.Add(loop[i]);
            }

            if (TryPointAtAngle(loop, to, origin, closed: true, out var end)) result.Add(end);

            result.Sort((a, b) => RelativeAngle(a, origin, from).CompareTo(RelativeAngle(b, origin, from)));
            return result;
        }

        /// <summary>
        /// Where a ray leaving <paramref name="centre"/> meets the loop. False only when the ray
        /// misses every edge, which a caller must treat as "this sector is empty" rather than
        /// substituting a point.
        /// </summary>
        public static bool TryPointAtAngle(IReadOnlyList<Vector2Value> loop, double angle,
            Vector2Value centre, bool closed, out Vector2Value point)
        {
            point = Vector2Value.Zero;
            if (loop.Count < 2) return false;

            var dirX = Math.Sin(angle);
            var dirY = Math.Cos(angle);

            var edges = closed ? loop.Count : loop.Count - 1;
            var bestDistance = double.NegativeInfinity;
            var found = false;

            for (var i = 0; i < edges; i++)
            {
                var from = loop[i];
                var to = loop[(i + 1) % loop.Count];

                double ax = from.X - centre.X, ay = from.Y - centre.Y;
                double ex = to.X - from.X, ey = to.Y - from.Y;

                // a + t (b - a) = s (dirX, dirY), solved for t by Cramer's rule.
                var denominator = ex * dirY - ey * dirX;
                if (Math.Abs(denominator) < 1e-12) continue;

                var t = (ay * dirX - ax * dirY) / denominator;
                if (t < -1e-6 || t > 1.0 + 1e-6) continue;

                // Clamped rather than merely accepted: a sample taken at a loop corner's own angle
                // lands a couple of ulps past the end of the edge that corner closes, and letting
                // that through unclamped moves the point off the loop.
                if (t < 0.0) t = 0.0;
                if (t > 1.0) t = 1.0;

                var hitX = ax + ex * t;
                var hitY = ay + ey * t;
                var distance = hitX * dirX + hitY * dirY;
                if (distance < -1e-9 || distance <= bestDistance) continue;

                bestDistance = distance;
                point = new Vector2Value((float)(hitX + centre.X), (float)(hitY + centre.Y));
                found = true;
            }

            return found;
        }

        #endregion

        #region Emitters

        /// <summary>
        /// Fans a convex polygon from its first corner into <paramref name="indices"/>. This is the
        /// filled case, closed or sliced alike - a sliced one simply carries the origin as the
        /// corner where its two cut edges meet.
        /// </summary>
        public static void AddFan(List<Vector2Value> vertices, List<int> indices,
            IReadOnlyList<Vector2Value> polygon)
        {
            if (polygon.Count < 3) return;

            var offset = vertices.Count;
            for (var i = 0; i < polygon.Count; i++) vertices.Add(polygon[i]);

            for (var i = 1; i + 1 < polygon.Count; i++)
            {
                indices.Add(offset);
                indices.Add(offset + i);
                indices.Add(offset + i + 1);
            }
        }

        // Both rims are resampled onto the UNION of their angles, not walked in lockstep: the rims pinch to
        // zero width where the polygon touches the box, and a plain merge emits a flipped triangle there.

        /// <summary>
        /// Fills the region between two loops - a ring, or an inverted shape, which is the same
        /// operation with the box as the outer loop. <paramref name="centre"/> must lie inside both:
        /// a ring uses the shape's own centroid, an inverted shape the box's centre.
        /// </summary>
        public static void AddAnnulus(List<Vector2Value> vertices, List<int> indices,
            IReadOnlyList<Vector2Value> outer, IReadOnlyList<Vector2Value> inner,
            bool closed, Vector2Value centre)
        {
            if (outer.Count < 2 || inner.Count < 2) return;

            var seam = ClockAngle(new Vector2Value(outer[0].X - centre.X, outer[0].Y - centre.Y));
            var angles = CollectAngles(outer, inner, centre, seam, closed);
            if (angles.Count < 2) return;

            var outerRing = new List<Vector2Value>(angles.Count);
            var innerRing = new List<Vector2Value>(angles.Count);

            for (var i = 0; i < angles.Count; i++)
            {
                if (!TryPointAtAngle(outer, seam + angles[i], centre, closed, out var outerPoint)) continue;
                if (!TryPointAtAngle(inner, seam + angles[i], centre, closed, out var innerPoint)) continue;

                outerRing.Add(outerPoint);
                innerRing.Add(innerPoint);
            }

            if (outerRing.Count < 2) return;

            var offset = vertices.Count;
            for (var i = 0; i < outerRing.Count; i++)
            {
                vertices.Add(outerRing[i]);
                vertices.Add(innerRing[i]);
            }

            for (var i = 0; i + 1 < outerRing.Count; i++)
            {
                int o0 = offset + i * 2, i0 = offset + i * 2 + 1;
                int o1 = offset + (i + 1) * 2, i1 = offset + (i + 1) * 2 + 1;

                indices.Add(o0); indices.Add(o1); indices.Add(i1);
                indices.Add(o0); indices.Add(i1); indices.Add(i0);
            }
        }

        private static List<double> CollectAngles(IReadOnlyList<Vector2Value> outer,
            IReadOnlyList<Vector2Value> inner, Vector2Value centre, double seam, bool closed)
        {
            var raw = new List<double>(outer.Count + inner.Count);
            for (var i = 0; i < outer.Count; i++) raw.Add(RelativeAngle(outer[i], centre, seam));
            for (var i = 0; i < inner.Count; i++) raw.Add(RelativeAngle(inner[i], centre, seam));
            raw.Sort();

            var angles = new List<double>(raw.Count + 2);
            for (var i = 0; i < raw.Count; i++)
            {
                if (angles.Count > 0 && Math.Abs(raw[i] - angles[angles.Count - 1]) < 1e-9) continue;
                if (closed && (raw[i] <= AngleEpsilon || raw[i] >= Tau - AngleEpsilon)) continue;
                angles.Add(raw[i]);
            }

            if (!closed) return angles;

            angles.Insert(0, 0.0);
            angles.Add(Tau);
            return angles;
        }

        #endregion
    }
}
