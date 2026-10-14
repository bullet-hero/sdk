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

namespace BH.SDK.Models
{
    // A new domain, not a bump: no existing model refers to it. The format, key by key, and the mask's bit
    // order: Docs/FILE_LAYOUT.md, "skins/".

    /// <summary> An avatar skin (<c>skins/&lt;id&gt;.json</c>): the body grid as a bit mask, the part each
    /// filled cell draws, the skin's own colours and meshes, and which other parts of the avatar it draws.
    /// A fresh instance is the classic avatar. </summary>
    [RuleContainer]
    [ModelGeneration(ModelDomains.AvatarSkin, ModelGenerations.V3_ImproveAppearance)]
    [GenerateModel]
    public sealed partial class AvatarSkin : IModel<AvatarSkin>
    {
        /// <summary> The classic grid's side, in cells. </summary>
        public const int ClassicSide = 5;

        /// <summary> Who this skin is, wherever its file goes; its file is named after it. </summary>
        [RuleIPrimitiveGuidNotNull]
        [JsonProperty(Names.Id)]
        public AvatarSkinId Id { get; set; }

        /// <summary> What the player calls it. </summary>
        [RuleNotNull, RuleStringMax(ValueRules.MaxEditorName)]
        [JsonProperty(Names.Name)]
        public string Name { get; set; }

        /// <summary> The body's drawn size against the classic one. Never touches the collider. </summary>
        [RuleInRange(AvatarSkinRules.MinBodySize, AvatarSkinRules.MaxBodySize)]
        [JsonProperty(Names.BodySize)]
        public float Size { get; set; }

        /// <summary> Cells across the grid. </summary>
        [RuleInRange(AvatarSkinRules.MinGridSide, AvatarSkinRules.MaxGridSide)]
        [JsonProperty(Names.Columns)]
        public int Columns { get; set; }

        /// <summary> Cells up the grid. </summary>
        [RuleInRange(AvatarSkinRules.MinGridSide, AvatarSkinRules.MaxGridSide)]
        [JsonProperty(Names.Rows)]
        public int Rows { get; set; }

        /// <summary> The filled cells: bit <c>row * 8 + column</c>, row 0 at the bottom, column 0 on the
        /// left. </summary>
        [JsonProperty(Names.Mask)]
        public ulong Mask { get; set; }

        /// <summary> The part each filled cell draws, an index into <see cref="Parts"/>, in ascending bit
        /// order. Empty draws part 0 in every cell. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Cells)]
        public List<int> Cells { get; set; }

        /// <summary> What a cell draws, addressed by <see cref="Cells"/>. </summary>
        [RuleNotNull, RuleCollectionMaxCount(AvatarSkinRules.MaxParts)]
        [JsonProperty(Names.Parts)]
        public List<AvatarSkinPart> Parts { get; set; }

        /// <summary> The body colours a part picks by <see cref="AvatarSkinPart.Color"/>. </summary>
        [RuleNotNull, RuleCollectionMaxCount(AvatarSkinRules.MaxColors)]
        [JsonProperty(Names.Colors)]
        public List<Color4Value> Colors { get; set; }

        /// <summary> Meshes this skin defines for itself, addressed by <see cref="AvatarSkinPart.Shape"/>.
        /// </summary>
        [RuleNotNull, RuleCollectionMaxCount(AvatarSkinRules.MaxMeshes)]
        [JsonProperty(Names.Meshes)]
        public List<AvatarSkinShape> Meshes { get; set; }

        /// <summary> Which parts of the avatar besides the grid are drawn. </summary>
        [RuleEnumFlagsValid]
        [JsonProperty(Names.Elements)]
        public AvatarSkinElements Elements { get; set; }

        /// <summary> The eyes. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Eyes)]
        public Color4Value Eyes { get; set; }

        /// <summary> The hands. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Hands)]
        public Color4Value Hands { get; set; }

        /// <summary> The shadow under the body. </summary>
        [RuleNotNull]
        [JsonProperty(Names.ShadowWord)]
        public Color4Value Shadow { get; set; }

        /// <summary> The trail left while moving. </summary>
        [RuleNotNull]
        [JsonProperty(Names.MoveEffect)]
        public Color4Value MoveEffect { get; set; }

        /// <summary> The trail left by a dash. </summary>
        [RuleNotNull]
        [JsonProperty(Names.DashEffect)]
        public Color4Value DashEffect { get; set; }

        /// <summary> The burst a hit throws off. </summary>
        [RuleNotNull]
        [JsonProperty(Names.DamageEffect)]
        public Color4Value DamageEffect { get; set; }

        /// <summary> What the body is tinted towards while dashing. </summary>
        [RuleNotNull]
        [JsonProperty(Names.DashBody)]
        public Color4Value DashBody { get; set; }

        /// <summary> The classic avatar: 5x5 squares in one colour, every part drawn. </summary>
        public AvatarSkin()
        {
            Id = AvatarSkinId.Null;
            Name = string.Empty;
            Size = 1f;
            Columns = ClassicSide;
            Rows = ClassicSide;
            Mask = AvatarSkinMask.Full(ClassicSide, ClassicSide);
            Cells = new List<int>();
            Parts = new List<AvatarSkinPart> { new() };
            Colors = new List<Color4Value> { AvatarSkinDefaults.Body() };
            Meshes = new List<AvatarSkinShape>();
            Elements = AvatarSkinElements.Classic;
            Eyes = AvatarSkinDefaults.Eyes();
            Hands = AvatarSkinDefaults.Hands();
            Shadow = AvatarSkinDefaults.Shadow();
            MoveEffect = AvatarSkinDefaults.MoveEffect();
            DashEffect = AvatarSkinDefaults.DashEffect();
            DamageEffect = AvatarSkinDefaults.DamageEffect();
            DashBody = AvatarSkinDefaults.DashBody();
        }
    }
}