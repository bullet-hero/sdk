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
