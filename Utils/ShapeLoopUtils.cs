using System;
using System.Collections.Generic;
using BH.SDK.Models.Values;

namespace BH.SDK.Utils
{
    // EVERY built-in shape is two closed loops and a sector, and this file is the whole vocabulary
    // for saying so. A filled shape is one loop; a ring is a loop with a second one inside it; an
    // inverted shape is the BOX's loop with the shape's loop inside it. Slicing is the same
    // operation on whichever loops are present. So the catalogue needs exactly two emitters - a fan
    // and an annulus - rather than a generator per combination, and adding an axis later means
    // producing different loops, not writing a fourth builder.
    //
    // ANGLES ARE MEASURED CLOCKWISE FROM STRAIGHT UP, and that is what makes the sectors fall out
    // for free: [0, pi] is the right half, [0, pi/2] the upper-right quarter, [pi/2, pi] the
    // lower-right one and [0, pi/4] the octant a 45-degree cut leaves. Those are the box's own axes
    // and diagonal through its centre, which is the rule the shipped library already follows (its
    // Pentagon_F4 is cut at y = 0.0503 and the pentagon's AABB centre is 0.0502) and the reason an
    // inverted slice can subtract the shape from the matching sector OF THE BOX: the same three
    // lines cut both.
    //
    // Deliberately separate from ShapeSynthUtils, which serves the Afterbeat importer's arbitrary
    // rounded polygons and reasons in the opposite angular convention (see CornerAngle there). The
    // two must not share a phase predicate.

    /// <summary>
    /// Radial loops - the intermediate form the built-in shape catalogue is built out of. A loop is
    /// a list of points ordered by their clockwise angle from straight up, star-shaped about the
    /// centre it is used with.
    /// </summary>
    public static class ShapeLoopUtils
    {
        /// <summary> Half-extent of the authored box. </summary>
        public const float BoxRadius = 0.5f;

        /// <summary> Circumradius of the box itself, i.e. the radius its four corners sit at. </summary>
        public const float BoxCornerRadius = 0.70710678f;

        public const double Tau = Math.PI * 2.0;

        // ANGLES ARE ALWAYS RELATIVE, never absolute, and this constant is why. A rim corner meant
        // to sit at angle zero is routinely built at x = -1e-17, whose absolute angle is not zero
        // but a hair under a full turn - which sorts it to the far end of its own loop and silently
        // reverses a sector. Measuring against a reference and snapping the result to zero is what
        // makes that impossible; comparing absolute angles is what made it happen.
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

        // The radius is picked so the BOUNDING BOX fills the unit box, not so every corner sits
        // within half a unit of the origin. Those are different numbers for an odd side count, and
        // the second one is what makes a triangle small: an equilateral triangle inscribed by its
        // circumradius reaches 0.5 up and only 0.433 sideways, while the shipped Triangle preset is
        // a full unit wide (its apex at 0.577 is outside the box only because the geometry is not
        // AABB-centred yet). Fitting the bounds keeps the shipped size and, once the loop is
        // recentred on its own bounds, puts every point back inside the box.

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

        public static List<Vector2Value> Translate(IReadOnlyList<Vector2Value> loop, Vector2Value offset)
        {
            var result = new List<Vector2Value>(loop.Count);
            for (var i = 0; i < loop.Count; i++)
                result.Add(new Vector2Value(loop[i].X + offset.X, loop[i].Y + offset.Y));
            return result;
        }

        // Shrinking towards the CORNER CENTROID rather than towards the origin, and for the right
        // triangle the difference is the whole shape: its hypotenuse runs through the origin, so an
        // origin-scaled copy keeps the same hypotenuse and the "ring" has no width along it at all.
        // For every regular polygon the two centres coincide and this changes nothing.

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

        // BOTH RIMS ARE RESAMPLED ONTO THE UNION OF THEIR OWN ANGLES, rather than walked in
        // lockstep, and that is not an optimisation to skip - it is what makes an inverted shape
        // correct. A regular polygon fitted to the box TOUCHES the box at its widest corners, so
        // the region between the two is pinched to zero width there and is not one ribbon at all.
        // A merge that advances whichever rim comes next straddles that pinch and emits a triangle
        // lying across the shape it was supposed to avoid - invisible in the signed area, obvious
        // as a flipped, overlapping triangle on screen. Sampling both rims at every angle either
        // one of them turns at makes each cell a proper trapezoid, and the pinch merely makes one
        // of them degenerate, which Sanitize drops.

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
