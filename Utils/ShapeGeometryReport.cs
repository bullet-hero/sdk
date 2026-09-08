using System.Text;

namespace BH.SDK.Utils
{
    /// <summary>
    /// What was wrong with a shape's geometry - produced either as a diagnosis
    /// (<see cref="ShapeGeometryUtils.Analyze"/>) or as a record of what was repaired
    /// (<see cref="ShapeGeometryUtils.Sanitize"/>). All zero means the shape is valid as authored.
    /// </summary>
    public struct ShapeGeometryReport
    {
        /// <summary> Vertices that were null in the file. </summary>
        public int NullVertices;

        /// <summary> Points outside the authored [-0.5, 0.5] box. </summary>
        public int OutOfBoundsPoints;

        /// <summary> Index triples that referenced no vertex, plus a trailing partial triple. </summary>
        public int MalformedIndices;

        /// <summary> Duplicate points merged into one. </summary>
        public int WeldedVertices;

        /// <summary> Triangles with no area - collapsed or collinear. </summary>
        public int DegenerateTriangles;

        /// <summary> Triangles beyond the format's cap. </summary>
        public int ExcessTriangles;

        /// <summary> Points beyond the format's cap. Sanitize never has to act on this directly:
        /// trimming triangles and dropping what they no longer reference brings the count under the
        /// cap on its own, since the cap IS three per capped triangle. </summary>
        public int ExcessVertices;

        /// <summary> Points no triangle referenced. </summary>
        public int OrphanVertices;

        /// <summary> Back-facing triangles whose winding was corrected. </summary>
        public int FlippedTriangles;

        /// <summary> True when the geometry needed no repair at all. </summary>
        public bool IsClean => NullVertices == 0 && OutOfBoundsPoints == 0 && MalformedIndices == 0
            && WeldedVertices == 0 && DegenerateTriangles == 0 && ExcessTriangles == 0
            && ExcessVertices == 0 && OrphanVertices == 0 && FlippedTriangles == 0;

        /// <summary> One line naming every non-zero finding, for an editor hint. Empty when clean. </summary>
        public string Describe()
        {
            if (IsClean) return string.Empty;

            var builder = new StringBuilder();
            Append(builder, FlippedTriangles, "triangle winding fixed");
            Append(builder, WeldedVertices, "duplicate points merged");
            Append(builder, OrphanVertices, "unconnected points removed");
            Append(builder, DegenerateTriangles, "empty triangles removed");
            Append(builder, OutOfBoundsPoints, "points pulled back into bounds");
            Append(builder, MalformedIndices, "broken triangles removed");
            Append(builder, ExcessTriangles, "triangles over the limit removed");
            Append(builder, ExcessVertices, "points over the limit removed");
            Append(builder, NullVertices, "missing points replaced");
            return builder.ToString();
        }

        private static void Append(StringBuilder builder, int count, string what)
        {
            if (count == 0) return;
            if (builder.Length > 0) builder.Append(", ");
            builder.Append(count).Append(' ').Append(what);
        }
    }
}
