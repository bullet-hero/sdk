using System;
using System.Collections.Generic;
using BH.SDK.Models.Attributes;
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
    // Stored INDEXED (vertices + indices): converts to a flat layout without guessing, while welding
    // back by epsilon does not. Why: Docs/RESOURCE_DATA.md

    /// <summary>
    /// A reusable shape built out of triangles, referenced by ShapeObject.ShapeId (drawn) and/or
    /// ColliderId (hit). Shared, so hundreds of identical bullets cost one definition.
    /// </summary>
    [RuleContainer]
    [RuleShapeGeometry]
    [ModelGeneration(ModelDomains.CompositeShape, ModelGenerations.V1_AlphaRelease)]
    [GenerateModel]
    public sealed partial class CompositeShape : IModel<CompositeShape>, IShapeGeometry
    {
        /// <summary> Identity of this shape - either a built-in id or a level-defined one. </summary>
        [RuleIPrimitiveGuidNotNull]
        [JsonProperty(Names.ShapeId)]
        public ShapeId ShapeId { get; set; }

        /// <summary> Editor-facing label of the shape. </summary>
        [RuleNotNull, RuleStringMax(ValueRules.MaxEditorName)]
        [JsonProperty(Names.ShapeName)]
        public string ShapeName { get; set; }

        // A shape carries NO pivot, by decision: RectObject.Pivot is the only one.
        // Why: Docs/RESOURCE_DATA.md

        // No collection rule beyond RuleNotNull: every generic collection fix is index-destructive
        // here, RuleShapeGeometry owns it. Why: Docs/RESOURCE_DATA.md

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

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public CompositeShape()
        {
            ShapeId = ShapeId.Null;
            ShapeName = string.Empty;
            Vertices = new List<Vector2Value>();
            Indices = new List<int>();
        }

        /// <summary> Built from its id, name, vertices and indices. </summary>
        public CompositeShape(ShapeId shapeId, string shapeName, List<Vector2Value> vertices, List<int> indices)
        {
            ShapeId = shapeId;
            ShapeName = shapeName;
            Vertices = vertices;
            Indices = indices;
        }
    }
}
