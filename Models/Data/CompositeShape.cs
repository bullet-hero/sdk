using System;
using System.Collections.Generic;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using BH.SDK.Versions;
using Newtonsoft.Json;

namespace BH.SDK.Models.Data
{
    // Stored INDEXED (vertices + indices) rather than as a flat triangle soup, because the same data
    // feeds two consumers that want opposite layouts: a render mesh wants shared vertices, collision
    // wants contiguous triples. Indexed is the form that converts to the other without guessing -
    // going the other way means welding by epsilon, and two triangles meeting on an edge do not
    // agree bit for bit once a person has dragged either of them.
    //
    // It is also what the shape editor edits: a corner shared by three triangles is ONE point that
    // moves once, instead of three that drift apart the moment one is dragged.

    /// <summary>
    /// A reusable shape built out of triangles, referenced by ShapeObject.ShapeId (what is drawn)
    /// and/or ShapeObject.ColliderId (what is hit). Shared rather than embedded, so hundreds of
    /// identical bullets cost one shape definition.
    /// </summary>
    [RuleContainer]
    [RuleShapeGeometry]
    [DataVersion(DataDomains.CompositeShape, 1, 0)]
    public class CompositeShape : IModel<CompositeShape>, IShapeGeometry
    {
        /// <summary> Identity of this shape - either a built-in id or a level-defined one. </summary>
        [RuleIPrimitiveGuidNotNull]
        [JsonProperty(Names.ShapeId)]
        public ShapeId ShapeId { get; set; }

        /// <summary> Editor-facing label of the shape. </summary>
        [RuleNotNull, RuleStringMax(ValueRules.MaxEditorName)]
        [JsonProperty(Names.ShapeName)]
        public string ShapeName { get; set; }

        // TODO (MAYBE) add Pivot for shape and maybe add it into collision process
        // TODO also most reason for it - extend game editor, because game shapes has it only for visuals
        // TODO or this can be PreferredPivot, it can be used by user in optional pivot in selection

        // Neither list carries a collection rule beyond RuleNotNull, and that is deliberate rather
        // than an omission: every generic collection fix is index-destructive here.
        // RuleCollectionNoNullItems would REMOVE a null vertex and shift every index after it onto
        // the wrong point; RuleCollectionMaxCount would truncate the vertex list out from under the
        // triangles still referencing its tail. Both repairs look local and corrupt the shape
        // silently. RuleShapeGeometry owns all of it instead, because only a rule that sees both
        // lists can fix one without breaking the other - it replaces nulls in place, and reaches the
        // vertex cap by trimming whole triangles and then dropping whatever is left unreferenced.

        /// <summary> Corners in the object's local rect space, each shared by every triangle that
        /// uses it. Concave shapes are expressed by using several triangles, never by winding
        /// order. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Vertices)]
        public List<Vector2Value> Vertices { get; set; }

        /// <summary> Triangle corners, three indices per triangle, pointing into
        /// <see cref="Vertices"/>. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Indices)]
        public List<int> Indices { get; set; }

        /// <summary> How many triangles this shape is made of. </summary>
        [JsonIgnore]
        public int TriangleCount => ShapeGeometryUtils.GetTriangleCount(Indices);

        public CompositeShape()
        {
            ShapeId = ShapeId.Null;
            ShapeName = string.Empty;
            Vertices = new List<Vector2Value>();
            Indices = new List<int>();
        }

        public CompositeShape(ShapeId shapeId, string shapeName, List<Vector2Value> vertices, List<int> indices)
        {
            ShapeId = shapeId;
            ShapeName = shapeName;
            Vertices = vertices;
            Indices = indices;
        }

        public void Reset()
        {
            ShapeId = ShapeId.Null;
            ShapeName = string.Empty;
            Vertices.Clear();
            Indices.Clear();
        }

        public object Clone() => Copy();
        public CompositeShape Copy() => new(ShapeId, ShapeName, Vertices.CopyList(), new List<int>(Indices));

        public void Update(CompositeShape src)
        {
            ShapeId = src.ShapeId;
            ShapeName = src.ShapeName;
            Vertices = src.Vertices.CopyList();
            Indices = new List<int>(src.Indices);
        }

        public void Pull(CompositeShape src)
        {
            ShapeId = src.ShapeId;
            ShapeName = src.ShapeName;
            Vertices = src.Vertices.CopyList();
            Indices = new List<int>(src.Indices);
        }

        public override bool Equals(object obj) => obj is CompositeShape value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(ShapeId, ShapeName,
            Vertices.GetListHashCode(), Indices.GetListHashCode());

        public bool Equals(CompositeShape other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = ShapeId.Equals(other.ShapeId)
                         && ShapeName.Equals(other.ShapeName)
                         && Vertices.ListEquals(other.Vertices)
                         && Indices.ListEquals(other.Indices);
            return result;
        }
    }
}
