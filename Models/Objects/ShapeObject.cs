using System;
using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Keyframes;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Objects
{
    /// <summary>
    /// The visible workhorse of a level: a rect that draws a shape and can hurt the player.
    /// Silhouette, image and hitbox are three separate concerns here - ShapeId is what is drawn,
    /// TextureResourceId is what is painted onto it, ColliderId is what is hit, and none of the
    /// three have to agree.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class ShapeObject : RectObject, IModel<ShapeObject>, IUpdatable<ShapeObject>
    {
        public override ObjectType GetModelType() => ObjectType.ShapeObject;

        // Two ShapeId fields, not one, and neither derives from the other. They answer different
        // questions - what the player SEES and what the player HITS - and a level routinely wants
        // them to disagree: a telegraph beam that is drawn but harmless, a hitbox simpler than the
        // art it guards, an invisible wall. Deriving one from the other would take that away and
        // buy nothing, since both are the same kind of data.

        /// <summary> Shape to draw, from the shared library. Null draws nothing at all - which,
        /// combined with a real ColliderId, is how an invisible hitbox is authored. </summary>
        [JsonProperty(Names.ShapeId)]
        public ShapeId ShapeId { get; set; }

        /// <summary> Collision shape from the shared library. Null means the object is decoration -
        /// drawn, never collided with. </summary>
        [JsonProperty(Names.ColliderId)]
        public ShapeId ColliderId { get; set; }

        /// <summary> Which render path to ask for. Auto lets the consumer decide from this object's
        /// own alpha, and is what every object gets until an author says otherwise. </summary>
        [RuleEnumValid(ShaderType.Auto)]
        [JsonProperty(Names.Shader)]
        public ShaderType ShaderType { get; set; }
        
        /// <summary> Image painted onto the shape. Null draws no image at all - the shape is filled
        /// with its own colour, which is what most objects want. </summary>
        [RuleReferenceExists(ResourceReferenceKind.Texture, true)]
        [JsonProperty(Names.TextureResourceId)]
        public TextureResourceId TextureResourceId { get; set; }

        /// <summary> Tint track, typed as the four-corner family - so a single object can be flat,
        /// horizontally, vertically or per-corner graded, and switch between those over time. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxObjectKeys)]
        [RuleCollectionUnique(nameof(IColor4X4Key.Frame))]
        [JsonProperty(Names.Color)]
        public List<IColor4X4Key> Colors { get; set; }

        /// <summary> Texture mapping track (tiling/offset) - animates the image inside the rect
        /// while the rect itself stays put. </summary>
        [RuleNotNull, RuleCollectionMaxCount(LevelRules.MaxObjectKeys)]
        [RuleCollectionUnique(nameof(UVKey.Frame))]
        [JsonProperty(Names.UV)]
        public List<UVKey> UVs { get; set; }

        public ShapeObject()
        {
            ShaderType = ShaderType.Auto;
            ShapeId = ShapeId.Square.Fill;
            ColliderId = ShapeId.Null;
            TextureResourceId = TextureResourceId.Null;
            Colors = new List<IColor4X4Key>();
            UVs = new List<UVKey>();
        }
        public ShapeObject(ObjectId objectId, ObjectId parentObjectId, string name, bool active, FrameSpan span, int layer,
            List<PosKey> positions, List<AngleKey> rotations, List<ScaKey> scales, List<ScaKey> sizes,
            List<AlignmentKey> anchorsMin, List<AlignmentKey> anchorsMax, List<AlignmentKey> pivots,
            ShaderType shaderType, ShapeId shapeId, ShapeId colliderId, TextureResourceId textureResourceId,
            List<IColor4X4Key> colors, List<UVKey> uvs)
            : base(objectId, parentObjectId, name, active, span, layer,
                positions, rotations, scales, sizes, anchorsMin, anchorsMax, pivots)
        {
            ShaderType = shaderType;
            ShapeId = shapeId;
            ColliderId = colliderId;
            TextureResourceId = textureResourceId;
            Colors = colors;
            UVs = uvs;
        }
    }
}
