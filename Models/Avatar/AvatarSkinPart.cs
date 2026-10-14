using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models
{
    /// <summary> What a cell of a skin draws: a shape - a catalogue one or one of the skin's own meshes -
    /// in one of the skin's colours, turned, mirrored and scaled inside the cell. </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class AvatarSkinPart : IModel<AvatarSkinPart>
    {
        /// <summary> A catalogue shape's id, or the id of one of <see cref="AvatarSkin.Meshes"/>. </summary>
        [JsonProperty(Names.ShapeWord)]
        public ShapeId Shape { get; set; }

        /// <summary> An index into <see cref="AvatarSkin.Colors"/>. </summary>
        [RuleInRange(0, AvatarSkinRules.MaxColors - 1)]
        [JsonProperty(Names.ColorWord)]
        public int Color { get; set; }

        /// <summary> Turn inside the cell, in degrees. </summary>
        [JsonProperty(Names.RotationWord)]
        public float Rotation { get; set; }

        /// <summary> Whether it is flipped left to right. </summary>
        [JsonProperty(Names.Mirror)]
        public bool Mirror { get; set; }

        /// <summary> Size inside the cell; 1 fills it. </summary>
        [RuleInRange(AvatarSkinRules.MinPartScale, AvatarSkinRules.MaxPartScale)]
        [JsonProperty(Names.ScaleWord)]
        public float Scale { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public AvatarSkinPart()
        {
            Shape = ShapeId.Square.Fill;
            Color = 0;
            Rotation = 0f;
            Mirror = false;
            Scale = 1f;
        }

        /// <summary> A shape in one colour. </summary>
        public AvatarSkinPart(ShapeId shape, int color) : this()
        {
            Shape = shape;
            Color = color;
        }
    }
}