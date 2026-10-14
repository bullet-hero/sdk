using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models
{
    // Stored indexed, like CompositeShape, and held to the same RuleShapeGeometry. The id is the skin's
    // own name for it; the game registers the mesh under a fresh id when the skin is worn.

    /// <summary> A mesh a skin defines for itself, in a cell's own 1x1 box. </summary>
    [RuleContainer]
    [RuleShapeGeometry]
    [GenerateModel]
    public sealed partial class AvatarSkinShape : IModel<AvatarSkinShape>, IShapeGeometry
    {
        /// <summary> What the skin's parts call it; never a catalogue shape's id. </summary>
        [JsonProperty(Names.Id)]
        public ShapeId Id { get; set; }

        /// <summary> Corners in the cell's box, each shared by every triangle that uses it. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Vertices)]
        public List<Vector2Value> Vertices { get; set; }

        /// <summary> Triangle corners, three indices per triangle, into <see cref="Vertices"/>. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Indices)]
        public List<int> Indices { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public AvatarSkinShape()
        {
            Id = ShapeId.Null;
            Vertices = new List<Vector2Value>();
            Indices = new List<int>();
        }

        /// <summary> Built from its id, vertices and indices. </summary>
        public AvatarSkinShape(ShapeId id, List<Vector2Value> vertices, List<int> indices)
        {
            Id = id;
            Vertices = vertices;
            Indices = indices;
        }
    }
}