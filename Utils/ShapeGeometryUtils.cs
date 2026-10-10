using System.Collections.Generic;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Values;
using BH.SDK.Rules;

namespace BH.SDK.Utils
{
    // The single implementation of a valid shape, shared by RuleShapeGeometryAttribute.Fix and the shape
    // editor's Save so they cannot drift. Unity-independent: the model side only (Docs/SHAPES.md).

    /// <summary>
    /// Geometry checks and repairs for a CompositeShape's indexed triangle data.
    /// </summary>
    public static class ShapeGeometryUtils
    {
        /// <summary> Grid the weld snaps to, matching the consuming project's own bake-time weld.
        /// Shapes live in [-0.5, 0.5], so 1e-5 is far finer than any authored detail. </summary>
        public const float WeldPrecision = 100000f;

        /// <summary> Below this a triangle has no area worth rendering or colliding against. Real
        /// triangles in this coordinate range measure in the 0.01-1.0 band, so this rejects only
        /// collapsed and collinear ones. </summary>
        public const float DegenerateEpsilon = 1e-6f;

        /// <summary> Signed double area. Positive is the winding this project renders front-facing;
        /// a negative one is culled away and the shape loses that triangle entirely. </summary>
        public static float SignedDoubleArea(Vector2Value a, Vector2Value b, Vector2Value c)
            => (b.X - a.X) * (c.Y - a.Y) - (c.X - a.X) * (b.Y - a.Y);

        /// <summary> True when three corners describe no area, so the triangle would draw nothing. </summary>
        public static bool IsDegenerate(Vector2Value a, Vector2Value b, Vector2Value c)
        {
            var area = SignedDoubleArea(a, b, c);
            return area < DegenerateEpsilon && area > -DegenerateEpsilon;
        }

        // Weighted by ABSOLUTE area (a back-facing triangle is a defect, not a hole), the general form of
        // GetCentroidPivot. The result is a PIVOT in [0, 1] while geometry lives in [-0.5, 0.5].

        /// <summary> Where this geometry balances, as a pivot. Returns the box centre for anything
        /// with no area at all, since nothing else is a more honest answer and a caller offering it
        /// as a preset then simply repeats the centre cell. </summary>
        public static Vector2Value GetCentroidPivot(IShapeGeometry geometry)
            => geometry == null
                ? new Vector2Value(0.5f, 0.5f)
                : GetCentroidPivot(geometry.Vertices, geometry.Indices);

        /// <summary> Where this geometry balances, as a pivot. </summary>
        public static Vector2Value GetCentroidPivot(List<Vector2Value> vertices, List<int> indices)
        {
            if (vertices == null || indices == null) return new Vector2Value(0.5f, 0.5f);

            double x = 0.0, y = 0.0, total = 0.0;
            for (var i = 0; i + 2 < indices.Count; i += 3)
            {
                var ia = indices[i];
                var ib = indices[i + 1];
                var ic = indices[i + 2];
                if (!IsValidIndex(ia, vertices.Count) || !IsValidIndex(ib, vertices.Count) ||
                    !IsValidIndex(ic, vertices.Count)) continue;

                var a = vertices[ia];
                var b = vertices[ib];
                var c = vertices[ic];
                if (a == null || b == null || c == null) continue;

                var weight = System.Math.Abs(SignedDoubleArea(a, b, c));
                if (weight < DegenerateEpsilon) continue;

                x += weight * (a.X + b.X + c.X) / 3.0;
                y += weight * (a.Y + b.Y + c.Y) / 3.0;
                total += weight;
            }

            if (total <= 0.0) return new Vector2Value(0.5f, 0.5f);
            return new Vector2Value((float)(0.5 + x / total), (float)(0.5 + y / total));
        }

        /// <summary> Triangle count implied by the index list. </summary>
        public static int GetTriangleCount(List<int> indices) => indices == null ? 0 : indices.Count / 3;

        /// <summary> Vertices referenced by no triangle. Such a point is invisible to both render and
        /// collision, so it is authoring debris rather than data. </summary>
        public static void FindOrphanVertices(List<Vector2Value> vertices, List<int> indices,
            List<int> orphans)
        {
            orphans.Clear();
            if (vertices == null || vertices.Count == 0) return;

            var used = new bool[vertices.Count];
            if (indices != null)
            {
                foreach (var index in indices)
                    if (index >= 0 && index < used.Length)
                        used[index] = true;
            }

            for (var i = 0; i < used.Length; i++)
                if (!used[i])
                    orphans.Add(i);
        }

        /// <summary> True when every triple indexes a real vertex and the list length is a multiple
        /// of three. </summary>
        public static bool AreIndicesWellFormed(List<Vector2Value> vertices, List<int> indices)
        {
            if (indices == null) return false;
            if (indices.Count % 3 != 0) return false;

            var vertexCount = vertices?.Count ?? 0;
            foreach (var index in indices)
                if (index < 0 || index >= vertexCount)
                    return false;

            return true;
        }

        /// <summary> How many triangles face away from the camera and would be culled. Zero is the
        /// invariant every stored shape satisfies. </summary>
        public static int CountBackFacing(List<Vector2Value> vertices, List<int> indices)
        {
            if (!AreIndicesWellFormed(vertices, indices)) return 0;

            var count = 0;
            for (var i = 0; i < indices.Count; i += 3)
            {
                if (SignedDoubleArea(vertices[indices[i]], vertices[indices[i + 1]], vertices[indices[i + 2]]) < 0f)
                    count++;
            }
            return count;
        }

        /// <summary> Everything wrong with a shape, in one call - what <see cref="Sanitize"/> would
        /// have to repair. All-zero means the shape is already valid. </summary>
        public static ShapeGeometryReport Analyze(List<Vector2Value> vertices, List<int> indices)
        {
            var report = new ShapeGeometryReport();
            if (vertices == null || indices == null) return report;

            if (indices.Count % 3 != 0) report.MalformedIndices = indices.Count % 3;

            // Duplicates are reported, not just repaired: Fix must not change data validation called clean.
            var seen = new HashSet<long>(vertices.Count);
            foreach (var vertex in vertices)
            {
                if (vertex == null) { report.NullVertices++; continue; }
                if (IsOutsideBox(vertex)) report.OutOfBoundsPoints++;
                if (!seen.Add(GetWeldKey(vertex))) report.WeldedVertices++;
            }

            for (var i = 0; i + 2 < indices.Count; i += 3)
            {
                var a = indices[i];
                var b = indices[i + 1];
                var c = indices[i + 2];
                if (!IsValidIndex(a, vertices.Count) || !IsValidIndex(b, vertices.Count) ||
                    !IsValidIndex(c, vertices.Count))
                {
                    report.MalformedIndices++;
                    continue;
                }

                var va = vertices[a];
                var vb = vertices[b];
                var vc = vertices[c];
                if (va == null || vb == null || vc == null) { report.MalformedIndices++; continue; }

                if (IsDegenerate(va, vb, vc)) report.DegenerateTriangles++;
                else if (SignedDoubleArea(va, vb, vc) < 0f) report.FlippedTriangles++;
            }

            var orphans = new List<int>();
            FindOrphanVertices(vertices, indices, orphans);
            report.OrphanVertices = orphans.Count;

            var triangles = GetTriangleCount(indices);
            if (triangles > ValueRules.MaxShapeTriangles)
                report.ExcessTriangles = triangles - ValueRules.MaxShapeTriangles;

            if (vertices.Count > ValueRules.MaxShapeVertices)
                report.ExcessVertices = vertices.Count - ValueRules.MaxShapeVertices;

            return report;
        }

        // The order is the one in which fixing one problem cannot reintroduce another: clamp, weld, then
        // degenerate, then winding last.

        /// <summary>
        /// Repairs a shape in place and reports what changed. Never throws, never half-repairs; an empty
        /// result means nothing survived and the caller refuses to store it.
        /// </summary>
        public static ShapeGeometryReport Sanitize(List<Vector2Value> vertices, List<int> indices)
        {
            var report = new ShapeGeometryReport();
            if (vertices == null || indices == null) return report;

            // Measured up front: by the time the passes below finish, the cap is satisfied as a side
            // effect of trimming triangles and dropping what they no longer reference, so there is
            // nothing left to count.
            if (vertices.Count > ValueRules.MaxShapeVertices)
                report.ExcessVertices = vertices.Count - ValueRules.MaxShapeVertices;

            report.NullVertices = ReplaceNullVertices(vertices);
            report.OutOfBoundsPoints = ClampPoints(vertices);
            report.MalformedIndices = DropMalformedTriples(vertices, indices);
            report.WeldedVertices = WeldPoints(vertices, indices);
            report.DegenerateTriangles = DropDegenerateTriangles(vertices, indices);
            report.ExcessTriangles = TrimToTriangleCap(indices);
            report.OrphanVertices = DropOrphanVertices(vertices, indices);
            report.FlippedTriangles = FixWinding(vertices, indices);

            return report;
        }

        /// <summary> Substitutes a zero vector for any null entry, so every later pass can assume a
        /// real point. A null here comes from hand-edited files, never from the editor. </summary>
        public static int ReplaceNullVertices(List<Vector2Value> vertices)
        {
            var count = 0;
            for (var i = 0; i < vertices.Count; i++)
            {
                if (vertices[i] != null) continue;
                vertices[i] = new Vector2Value(0f, 0f);
                count++;
            }
            return count;
        }

        /// <summary> Pulls every point into the authored box. </summary>
        public static int ClampPoints(List<Vector2Value> vertices)
        {
            var count = 0;
            foreach (var vertex in vertices)
            {
                if (!IsOutsideBox(vertex)) continue;
                vertex.X = BHSDKMath.Clamp(vertex.X, ValueRules.MinShapePoint, ValueRules.MaxShapePoint);
                vertex.Y = BHSDKMath.Clamp(vertex.Y, ValueRules.MinShapePoint, ValueRules.MaxShapePoint);
                count++;
            }
            return count;
        }

        /// <summary> Drops triples that index nothing, plus any trailing partial triple. </summary>
        public static int DropMalformedTriples(List<Vector2Value> vertices, List<int> indices)
        {
            var kept = new List<int>(indices.Count);
            var dropped = 0;

            for (var i = 0; i + 2 < indices.Count; i += 3)
            {
                var a = indices[i];
                var b = indices[i + 1];
                var c = indices[i + 2];
                if (!IsValidIndex(a, vertices.Count) || !IsValidIndex(b, vertices.Count) ||
                    !IsValidIndex(c, vertices.Count))
                {
                    dropped++;
                    continue;
                }
                kept.Add(a);
                kept.Add(b);
                kept.Add(c);
            }

            // A trailing 1-2 index remainder is malformed on its own, even when every index in it
            // resolves - it describes no triangle.
            if (indices.Count % 3 != 0) dropped++;

            indices.Clear();
            indices.AddRange(kept);
            return dropped;
        }

        /// <summary> Merges points sharing a <see cref="WeldPrecision"/> grid cell and remaps the
        /// indices onto the survivors. This is what makes a shared corner one point the author can
        /// drag once, instead of three that quietly drift apart. </summary>
        public static int WeldPoints(List<Vector2Value> vertices, List<int> indices)
        {
            var lookup = new Dictionary<long, int>(vertices.Count);
            var remap = new int[vertices.Count];
            var welded = new List<Vector2Value>(vertices.Count);

            for (var i = 0; i < vertices.Count; i++)
            {
                var key = GetWeldKey(vertices[i]);
                if (lookup.TryGetValue(key, out var existing))
                {
                    remap[i] = existing;
                    continue;
                }

                remap[i] = welded.Count;
                lookup.Add(key, welded.Count);
                welded.Add(vertices[i]);
            }

            var removed = vertices.Count - welded.Count;
            if (removed == 0) return 0;

            for (var i = 0; i < indices.Count; i++)
                indices[i] = remap[indices[i]];

            vertices.Clear();
            vertices.AddRange(welded);
            return removed;
        }

        /// <summary> Removes triangles with no area - collapsed by a weld, or authored collinear. </summary>
        public static int DropDegenerateTriangles(List<Vector2Value> vertices, List<int> indices)
        {
            var kept = new List<int>(indices.Count);
            var dropped = 0;

            for (var i = 0; i + 2 < indices.Count; i += 3)
            {
                var a = indices[i];
                var b = indices[i + 1];
                var c = indices[i + 2];
                if (a == b || b == c || a == c || IsDegenerate(vertices[a], vertices[b], vertices[c]))
                {
                    dropped++;
                    continue;
                }
                kept.Add(a);
                kept.Add(b);
                kept.Add(c);
            }

            indices.Clear();
            indices.AddRange(kept);
            return dropped;
        }

        /// <summary> Cuts the triangle list down to the format's cap, keeping the earliest ones. </summary>
        public static int TrimToTriangleCap(List<int> indices)
        {
            var triangles = GetTriangleCount(indices);
            if (triangles <= ValueRules.MaxShapeTriangles) return 0;

            var excess = triangles - ValueRules.MaxShapeTriangles;
            indices.RemoveRange(ValueRules.MaxShapeTriangles * 3, excess * 3);
            return excess;
        }

        /// <summary> Removes points no triangle uses and compacts the indices onto what remains. </summary>
        public static int DropOrphanVertices(List<Vector2Value> vertices, List<int> indices)
        {
            var orphans = new List<int>();
            FindOrphanVertices(vertices, indices, orphans);
            if (orphans.Count == 0) return 0;

            var remap = new int[vertices.Count];
            var kept = new List<Vector2Value>(vertices.Count - orphans.Count);
            var orphanSet = new HashSet<int>(orphans);

            for (var i = 0; i < vertices.Count; i++)
            {
                if (orphanSet.Contains(i))
                {
                    remap[i] = -1;
                    continue;
                }
                remap[i] = kept.Count;
                kept.Add(vertices[i]);
            }

            for (var i = 0; i < indices.Count; i++)
                indices[i] = remap[indices[i]];

            vertices.Clear();
            vertices.AddRange(kept);
            return orphans.Count;
        }

        /// <summary> Swaps the last two corners of every back-facing triangle. Collision does not
        /// care which way a triangle faces, rendering does - a flipped one is culled and simply
        /// missing, which reads as broken geometry rather than as a winding problem. </summary>
        public static int FixWinding(List<Vector2Value> vertices, List<int> indices)
        {
            var fixedCount = 0;
            for (var i = 0; i + 2 < indices.Count; i += 3)
            {
                if (SignedDoubleArea(vertices[indices[i]], vertices[indices[i + 1]], vertices[indices[i + 2]]) >= 0f)
                    continue;

                (indices[i + 1], indices[i + 2]) = (indices[i + 2], indices[i + 1]);
                fixedCount++;
            }
            return fixedCount;
        }

        private static bool IsOutsideBox(Vector2Value vertex)
            => vertex.X < ValueRules.MinShapePoint || vertex.X > ValueRules.MaxShapePoint
                || vertex.Y < ValueRules.MinShapePoint || vertex.Y > ValueRules.MaxShapePoint;

        private static bool IsValidIndex(int index, int vertexCount)
            => index >= 0 && index < vertexCount;

        private static long GetWeldKey(Vector2Value vertex)
        {
            var x = (long)System.Math.Round(vertex.X * WeldPrecision);
            var y = (long)System.Math.Round(vertex.Y * WeldPrecision);
            return (x << 32) ^ (y & 0xFFFFFFFFL);
        }
    }
}
