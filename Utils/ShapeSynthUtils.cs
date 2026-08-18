using System;
using System.Collections.Generic;
using BH.SDK.Models.Data;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Rules;

namespace BH.SDK.Utils
{
    // These build the shapes the game does NOT ship as presets. The built-in library is exactly the
    // 78 ids ShapeId names, and anything outside it - an arrow, a half ring, a shape some other
    // game's format has and this one does not - arrives as level-authored data instead of growing
    // that library. A level carrying its own geometry already works everywhere; a 79th preset would
    // have to exist in every build that ever opens the file.
    //
    // Everything here ends with ShapeGeometryUtils.Sanitize, which is the same call RuleShapeGeometry
    // .Fix makes. That is what lets a generator be written as plain trigonometry: winding, welding,
    // the point box and both caps are somebody else's problem by construction, and a shape this file
    // returns is by definition one the format accepts.

    /// <summary>
    /// Procedural <see cref="CompositeShape"/> geometry, built from parameters rather than authored.
    /// Every result lives in the format's own [-0.5, 0.5] box and is sanitized before it is returned.
    /// </summary>
    public static class ShapeSynthUtils
    {
        /// <summary> Half-extent of the authored box; every generator inscribes itself in it. </summary>
        public const float Radius = 0.5f;

        /// <summary> Fewest sides a closed ring/polygon can have. </summary>
        public const int MinSides = 3;

        /// <summary> A ring costs two triangles per side, so this is the triangle cap halved. </summary>
        public const int MaxRingSides = ValueRules.MaxShapeTriangles / 2;

        #region Closed shapes

        /// <summary> Filled regular polygon, inscribed in the unit box, first corner pointing up. </summary>
        public static CompositeShape Polygon(ShapeId shapeId, string name, int sides)
        {
            sides = BHSDKMath.Clamp(sides, MinSides, ValueRules.MaxShapeTriangles);
            return Wedge(shapeId, name, sides, 1f);
        }

        /// <summary>
        /// Filled slice of a regular polygon - a half, quarter or eighth "circle". <paramref name="turns"/>
        /// is the fraction of a full turn the slice covers, in (0, 1].
        /// </summary>
        public static CompositeShape Wedge(ShapeId shapeId, string name, int sides, float turns)
        {
            sides = BHSDKMath.Clamp(sides, MinSides, ValueRules.MaxShapeTriangles);
            turns = BHSDKMath.Clamp(turns, 0.001f, 1f);

            var full = turns >= 1f;
            var segments = full ? sides : Math.Max(1, (int)Math.Round(sides * turns));
            var step = (turns * 2.0 * Math.PI) / segments;

            var vertices = new List<Vector2Value>(segments + 2) { new(0f, 0f) };
            var rim = full ? segments : segments + 1;
            for (var i = 0; i < rim; i++)
                vertices.Add(OnCircle(i * step, Radius));

            var indices = new List<int>(segments * 3);
            for (var i = 0; i < segments; i++)
            {
                var a = 1 + i;
                var b = 1 + (i + 1) % rim;
                indices.Add(0);
                indices.Add(b);
                indices.Add(a);
            }

            return Build(shapeId, name, vertices, indices);
        }

        /// <summary>
        /// Outline of a regular polygon. <paramref name="thickness"/> is the ring's width as a
        /// fraction of the radius, in (0, 1) - larger is a fatter ring, 1 would be the filled shape.
        /// </summary>
        public static CompositeShape Ring(ShapeId shapeId, string name, int sides, float thickness)
            => RingWedge(shapeId, name, sides, thickness, 1f);

        /// <summary>
        /// Outline of a slice of a regular polygon - the combination the built-in library has no
        /// preset for, e.g. a half-circle outline.
        /// </summary>
        public static CompositeShape RingWedge(ShapeId shapeId, string name, int sides,
            float thickness, float turns)
        {
            sides = BHSDKMath.Clamp(sides, MinSides, MaxRingSides);
            thickness = BHSDKMath.Clamp(thickness, 0.01f, 0.99f);
            turns = BHSDKMath.Clamp(turns, 0.001f, 1f);

            var full = turns >= 1f;
            var segments = full ? sides : Math.Max(1, (int)Math.Round(sides * turns));
            var step = (turns * 2.0 * Math.PI) / segments;
            var inner = Radius * (1f - thickness);

            // A closed ring reuses its first pair to shut the loop; an open one needs its own last
            // pair, or the arc silently wraps back to the start and fills the hole it was cut for.
            var rim = full ? segments : segments + 1;

            var vertices = new List<Vector2Value>(rim * 2);
            for (var i = 0; i < rim; i++)
            {
                var angle = i * step;
                vertices.Add(OnCircle(angle, Radius));
                vertices.Add(OnCircle(angle, inner));
            }

            var indices = new List<int>(segments * 6);
            for (var i = 0; i < segments; i++)
            {
                var o0 = (i * 2) % vertices.Count;
                var i0 = (i * 2 + 1) % vertices.Count;
                var o1 = ((i + 1) * 2) % vertices.Count;
                var i1 = ((i + 1) * 2 + 1) % vertices.Count;

                indices.Add(o0); indices.Add(i1); indices.Add(o1);
                indices.Add(o0); indices.Add(i0); indices.Add(i1);
            }

            return Build(shapeId, name, vertices, indices);
        }

        #endregion

        #region Rounded shapes

        // A polygon with ROUNDED CORNERS, which is a different generator rather than a nicety: each
        // corner becomes a quadratic Bezier fillet, so the point count multiplies and a six-sided
        // shape stops reading as a hexagon and starts reading as a squircle. It exists because a
        // foreign format has it - Afterbeat's custom polygon rounds by default - and building those
        // sharp is the difference between a shape that is recognisably the same and one that is
        // recognisably blockier, which no amount of side count fixes.
        //
        // THE FILLET RESOLUTION IS NOT FIXED, and cannot be. This format caps a shape at
        // ValueRules.MaxShapeTriangles, and a filleted corner costs points per corner on both loops
        // of a ring - so a twelve-sided rounded ring at five points a corner wants 120 triangles for
        // a 64-triangle budget. Rather than refuse the shape or silently drop the rounding, the
        // fillet is thinned until it fits: five points a corner where there is room, fewer where
        // there is not, one being a sharp corner again. A shape that has to degrade degrades in
        // smoothness, which is the least visible thing about it.

        /// <summary> Points a fully resolved corner fillet is drawn with. </summary>
        public const int MaxFilletPoints = 5;

        /// <summary>
        /// Rounded regular polygon, ring or slice - the general case of all three. A
        /// <paramref name="thickness"/> of 1 is filled and anything less is a ring of that width;
        /// <paramref name="turns"/> is the fraction of a full turn covered, 1 being closed;
        /// <paramref name="roundness"/> is the fillet size as a fraction of the radius, 0 being
        /// sharp corners. <paramref name="radius"/> lets a caller inscribe the shape by something
        /// other than its circumradius, and is scaled down when the result would leave the box.
        /// </summary>
        public static CompositeShape RoundedShape(ShapeId shapeId, string name, int sides,
            float roundness, float thickness, float turns,
            float radius = Radius, bool halfStepPhase = false)
        {
            sides = BHSDKMath.Clamp(sides, MinSides, ValueRules.MaxShapeTriangles);
            roundness = BHSDKMath.Clamp(roundness, 0f, 1f);
            thickness = BHSDKMath.Clamp(thickness, 0.01f, 1f);
            turns = BHSDKMath.Clamp(turns, 0.001f, 1f);
            radius = FitRadius(radius, sides, halfStepPhase);

            var filled = thickness >= 1f;
            var closed = turns >= 1f;
            var segments = closed ? sides : Math.Max(1, (int)Math.Round(sides * turns));

            var fillet = ResolveFilletPoints(roundness, segments, filled);
            var outer = BuildRim(sides, segments, closed, fillet, roundness, radius, halfStepPhase);
            if (outer.Count < ValueRules.MinShapeVertices && !filled) return null;

            return filled
                ? BuildFan(shapeId, name, outer, closed)
                : BuildStrip(shapeId, name, outer,
                    BuildRim(sides, segments, closed, fillet, roundness * (1f - thickness),
                        radius * (1f - thickness), halfStepPhase),
                    closed);
        }

        // A polygon is inscribed by its CIRCUMRADIUS, so the radius that makes it fill the box is
        // not the same number for every side count - a square needs sqrt(2)/2 to have its corners on
        // the box, a hexagon needs exactly a half. A caller passing a radius the box cannot hold
        // gets the shape scaled to fit rather than clipped: Sanitize clamps a stray point onto the
        // boundary, which flattens the corner it belonged to instead of shrinking the shape.
        /// <summary> The radius a polygon can actually be drawn at without leaving the box - the
        /// requested one, or as much of it as fits. Public because a caller that has to compensate
        /// for the shrink elsewhere needs to know it happened. </summary>
        public static float FitRadius(float radius, int sides, bool halfStepPhase)
        {
            radius = BHSDKMath.Clamp(radius, 0.01f, 4f);

            var extent = 0.0;
            for (var i = 0; i < sides; i++)
            {
                var angle = CornerAngle(i, sides, halfStepPhase);
                extent = Math.Max(extent, Math.Abs(Math.Sin(angle)));
                extent = Math.Max(extent, Math.Abs(Math.Cos(angle)));
            }

            var reach = radius * (float)extent;
            return reach > Radius ? radius * (Radius / reach) : radius;
        }

        // Where a rounded polygon's corner i sits, in the angle OnCircle takes - which measures from
        // straight up and turns clockwise, while every polygon convention worth matching measures
        // from straight down and turns anticlockwise. Hence the half turn and the subtraction: they
        // are the change of basis between the two, not a fudge.
        //
        // Both halves matter and for different reasons. The HALF STEP is what puts a corner at the
        // top of an odd-sided shape - a triangle built without it points down, and the set of
        // corners is only symmetric enough to hide that on even side counts. The DIRECTION only
        // shows on a slice: a full polygon covers the same corners either way round, while a slice
        // starting at the same corner and sweeping the other way is the mirror sector.
        private static double CornerAngle(int index, int sides, bool halfStepPhase)
        {
            var step = 2.0 * Math.PI / sides;
            var start = Math.PI - (halfStepPhase ? step * 0.5 : 0.0);
            return start - index * step;
        }

        private static int ResolveFilletPoints(float roundness, int segments, bool filled)
        {
            if (roundness <= 0f) return 1;

            // A filled fan costs one triangle per rim point; a ring costs two.
            var budget = filled ? ValueRules.MaxShapeTriangles : ValueRules.MaxShapeTriangles / 2;
            var affordable = segments > 0 ? budget / segments : MaxFilletPoints;
            return BHSDKMath.Clamp(affordable, 1, MaxFilletPoints);
        }

        // One loop of the shape. The corners themselves are the polygon's; what is emitted per
        // corner is either the corner (sharp) or a fillet across it, and an OPEN shape keeps both of
        // its cut ends sharp - they are where the slice was made, not corners of the polygon.
        private static List<Vector2Value> BuildRim(int sides, int segments, bool closed,
            int filletPoints, float roundness, float radius, bool halfStepPhase)
        {
            var corners = new List<Vector2Value>(segments + 1);
            for (var i = 0; i <= segments; i++)
                corners.Add(OnCircle(CornerAngle(i, sides, halfStepPhase), radius));

            var rim = new List<Vector2Value>((segments + 1) * filletPoints);
            var inset = radius * roundness;

            for (var i = 0; i < segments; i++)
            {
                var corner = corners[i];
                if (filletPoints <= 1 || inset <= 0f || (!closed && i == 0))
                {
                    rim.Add(corner);
                    continue;
                }

                var previous = closed ? corners[(i - 1 + segments) % segments] : corners[i - 1];
                var next = corners[i + 1];

                var from = Towards(corner, previous, inset);
                var to = Towards(corner, next, inset);

                for (var k = 0; k < filletPoints; k++)
                    rim.Add(QuadraticBezier(from, corner, to, k / (float)(filletPoints - 1)));
            }

            if (!closed) rim.Add(corners[segments]);
            return rim;
        }

        private static CompositeShape BuildFan(ShapeId shapeId, string name,
            List<Vector2Value> rim, bool closed)
        {
            var vertices = new List<Vector2Value>(rim.Count + 1) { new(0f, 0f) };
            vertices.AddRange(rim);

            var spans = closed ? rim.Count : rim.Count - 1;
            var indices = new List<int>(spans * 3);
            for (var i = 0; i < spans; i++)
            {
                indices.Add(0);
                indices.Add(1 + (i + 1) % rim.Count);
                indices.Add(1 + i);
            }
            return Build(shapeId, name, vertices, indices);
        }

        private static CompositeShape BuildStrip(ShapeId shapeId, string name,
            List<Vector2Value> outer, List<Vector2Value> inner, bool closed)
        {
            var count = Math.Min(outer.Count, inner.Count);
            if (count < 2) return null;

            var vertices = new List<Vector2Value>(count * 2);
            for (var i = 0; i < count; i++)
            {
                vertices.Add(outer[i]);
                vertices.Add(inner[i]);
            }

            var spans = closed ? count : count - 1;
            var indices = new List<int>(spans * 6);
            for (var i = 0; i < spans; i++)
            {
                var o0 = i * 2;
                var i0 = i * 2 + 1;
                var o1 = (i + 1) % count * 2;
                var i1 = (i + 1) % count * 2 + 1;

                indices.Add(o0); indices.Add(i1); indices.Add(o1);
                indices.Add(o0); indices.Add(i0); indices.Add(i1);
            }
            return Build(shapeId, name, vertices, indices);
        }

        private static Vector2Value Towards(Vector2Value from, Vector2Value to, float distance)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            var length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length <= float.Epsilon) return new Vector2Value(from.X, from.Y);

            var scale = Math.Min(distance, length * 0.5f) / length;
            return new Vector2Value(from.X + dx * scale, from.Y + dy * scale);
        }

        private static Vector2Value QuadraticBezier(Vector2Value a, Vector2Value b, Vector2Value c, float t)
        {
            var u = 1f - t;
            return new Vector2Value(
                u * u * a.X + 2f * u * t * b.X + t * t * c.X,
                u * u * a.Y + 2f * u * t * b.Y + t * t * c.Y);
        }

        #endregion

        #region Arrows

        /// <summary>
        /// A full arrow pointing up: a rectangular shaft under a triangular head.
        /// <paramref name="headLength"/> and the two widths are fractions of the box, in (0, 1].
        /// </summary>
        public static CompositeShape Arrow(ShapeId shapeId, string name,
            float headLength = 0.45f, float headWidth = 1f, float shaftWidth = 0.35f)
        {
            headLength = BHSDKMath.Clamp(headLength, 0.05f, 1f);
            headWidth = BHSDKMath.Clamp(headWidth, 0.05f, 1f);
            shaftWidth = BHSDKMath.Clamp(shaftWidth, 0.01f, headWidth);

            var top = Radius;
            var bottom = -Radius;
            var neck = top - headLength * (Radius * 2f);
            var half = shaftWidth * Radius;
            var headHalf = headWidth * Radius;

            var vertices = new List<Vector2Value>(7)
            {
                new(-half, bottom), // 0
                new(half, bottom),  // 1
                new(half, neck),    // 2
                new(-half, neck),   // 3
                new(-headHalf, neck), // 4
                new(headHalf, neck),  // 5
                new(0f, top),         // 6
            };
            var indices = new List<int> { 0, 1, 2, 0, 2, 3, 4, 5, 6 };

            return Build(shapeId, name, vertices, indices);
        }

        /// <summary> The head of an <see cref="Arrow"/> on its own - a triangle pointing up, filling
        /// the box. Its own shape is a Triangle preset's; it exists so the two arrows read as one
        /// family at the call site. </summary>
        public static CompositeShape ArrowHead(ShapeId shapeId, string name, float headWidth = 1f)
        {
            headWidth = BHSDKMath.Clamp(headWidth, 0.05f, 1f);
            var headHalf = headWidth * Radius;

            var vertices = new List<Vector2Value>(3)
            {
                new(-headHalf, -Radius),
                new(headHalf, -Radius),
                new(0f, Radius),
            };
            var indices = new List<int> { 0, 1, 2 };

            return Build(shapeId, name, vertices, indices);
        }

        #endregion

        #region Building blocks

        /// <summary> Axis-aligned rectangle centred on the origin; the sizes are fractions of the box. </summary>
        public static CompositeShape Rect(ShapeId shapeId, string name, float width = 1f, float height = 1f)
        {
            var halfW = BHSDKMath.Clamp(width, 0.01f, 1f) * Radius;
            var halfH = BHSDKMath.Clamp(height, 0.01f, 1f) * Radius;

            var vertices = new List<Vector2Value>(4)
            {
                new(-halfW, -halfH), new(halfW, -halfH), new(halfW, halfH), new(-halfW, halfH),
            };
            var indices = new List<int> { 0, 1, 2, 0, 2, 3 };

            return Build(shapeId, name, vertices, indices);
        }

        /// <summary> Wraps raw geometry into a sanitized shape. Returns null when nothing survived
        /// sanitizing, which a caller must treat as "this shape cannot be built" rather than storing
        /// an empty one. </summary>
        public static CompositeShape Build(ShapeId shapeId, string name,
            List<Vector2Value> vertices, List<int> indices)
        {
            ShapeGeometryUtils.Sanitize(vertices, indices);
            if (vertices.Count < ValueRules.MinShapeVertices) return null;
            if (ShapeGeometryUtils.GetTriangleCount(indices) < ValueRules.MinShapeTriangles) return null;

            return new CompositeShape(shapeId, name ?? string.Empty, vertices, indices);
        }

        // Angle 0 is straight up rather than to the right, so a "half circle" reads as the top half
        // and an arrow points the way its name says. Every preset in the built-in library is
        // authored the same way.
        private static Vector2Value OnCircle(double angle, float radius)
            => new((float)(Math.Sin(angle) * radius), (float)(Math.Cos(angle) * radius));

        #endregion
    }
}
