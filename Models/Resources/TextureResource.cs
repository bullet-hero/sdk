using System;
using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Resources
{
    /// <summary>
    /// An image the level brings with it. The only Resource subtype with extra data of its own -
    /// a sub-rect, so one shipped atlas can back many different sprites.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class TextureResource : Resource, IModel<TextureResource>
    {
        /// <summary> Identity of this image within the level. </summary>
        [RuleIPrimitiveIntMax(TextureResourceId.MaxUserDefinedValue)]
        [JsonProperty(Names.TextureResourceId)]
        public TextureResourceId TextureResourceId { get; set; }

        /// <summary> Region of the source image this resource actually is, as tiling+offset. Applies
        /// to the resource itself, unlike UVKey which animates a single object's mapping. </summary>
        [RuleNotNull]
        [JsonProperty(Names.TextureResourceUV)]
        public Vector4Value TextureResourceUV { get; set; }

        // SIX FIELDS, SIX INDEPENDENT AXES, and none of them is a format, a size or a memory budget.
        // A level has to play the same on every device, so the author says what is true of the
        // PICTURE and the player's own settings (UserSettings.Graphics.Textures) decide what this
        // device does about it; Core's TextureLoadPlanner is the only place the two meet.
        //
        // Kind names what the picture is. Alpha answers the one question about it that a device can
        // read the file and still not know (see TextureAlpha's own header). Sampling says whether it
        // is drawn crisp or blurred, which costs the same on every GPU and is therefore a look rather
        // than a budget. Compression says whether a lossy format may be used at all - the veto Kind
        // used to carry on its own behalf. WrapU/WrapV say what lies outside its edges, per axis,
        // which is composition and has no player half at all.
        //
        // WHY SIX RATHER THAN ONE RICHER Kind: they genuinely vary independently, and every pair that
        // was ever folded together made a real case unsayable. An opaque pixel-art tile repeating
        // horizontally and clamped vertically is five answers; a photograph an author refuses to have
        // compressed is not a Gradient; a crisp hand-drawn sprite is not pixel art and must not
        // silently lose its mip-maps for wanting hard edges. Kind still SEEDS Sampling and
        // Compression, so nothing authored before them changed.
        //
        // Every one is additive with a zero default except WrapU/WrapV, which REPLACED a single Wrap
        // under a new pair of keys - a level written before them reads back Clamp on both axes, which
        // costs a re-save on any level that had authored a repeat (Rule 11: the format breaks in
        // place before release). LevelResources stays at generation 1 either way.

        /// <summary> What this image is - a photo, a drawing, pixel art - so the device can treat it
        /// the way that kind of picture has to be treated. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.Kind)]
        public TextureKind Kind { get; set; }

        /// <summary> Whether this image uses the alpha channel its file carries. Nothing verifies
        /// the claim - see <see cref="TextureAlpha"/>. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.Alpha)]
        public TextureAlpha Alpha { get; set; }

        /// <summary> Whether this image is drawn crisp or blurred. Artistic - it costs the same
        /// either way - so it outranks the player's own filtering. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.Sampling)]
        public TextureSampling Sampling { get; set; }

        /// <summary> Whether a device may pack this image into a lossy format. Never touches the size
        /// cap - see <see cref="TextureCompressionKind"/>. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.Compression)]
        public TextureCompressionKind Compression { get; set; }

        /// <summary> What this image does past its left and right edges, which is what makes
        /// <see cref="TextureResourceUV"/>'s tiling half mean anything. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.WrapU)]
        public TextureWrapKind WrapU { get; set; }

        /// <summary> What this image does past its top and bottom edges. Separate from
        /// <see cref="WrapU"/> because a strip that tiles along one axis only is ordinary content.
        /// </summary>
        [RuleEnumValid]
        [JsonProperty(Names.WrapV)]
        public TextureWrapKind WrapV { get; set; }

        /// <summary> Which kind of resource this is. </summary>
        [JsonProperty(Names.Type)]
        public override ResourceType Type => ResourceType.Texture;

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public TextureResource()
        {
            TextureResourceId = TextureResourceId.Null;
            TextureResourceUV = new Vector4Value(ValueRules.DefaultUvX,
                ValueRules.DefaultUvY, ValueRules.DefaultUvZ, ValueRules.DefaultUvW);
            Kind = TextureKind.Auto;
            Alpha = TextureAlpha.Auto;
            Sampling = TextureSampling.Auto;
            Compression = TextureCompressionKind.Auto;
            WrapU = TextureWrapKind.Clamp;
            WrapV = TextureWrapKind.Clamp;
        }

        /// <summary> Built from its resource id and sources. </summary>
        public TextureResource(TextureResourceId textureResourceId, List<ResourceKey> sources) : base(sources)
        {
            TextureResourceId = textureResourceId;
            TextureResourceUV = new Vector4Value(ValueRules.DefaultUvX,
                ValueRules.DefaultUvY, ValueRules.DefaultUvZ, ValueRules.DefaultUvW);
            Kind = TextureKind.Auto;
            Alpha = TextureAlpha.Auto;
            Sampling = TextureSampling.Auto;
            Compression = TextureCompressionKind.Auto;
            WrapU = TextureWrapKind.Clamp;
            WrapV = TextureWrapKind.Clamp;
        }

        /// <summary> Every member at once, in declaration order. </summary>
        public TextureResource(TextureResourceId textureResourceId, Vector4Value textureResourceUV,
            List<ResourceKey> sources) : base(sources)
        {
            TextureResourceId = textureResourceId;
            TextureResourceUV = textureResourceUV;
            Kind = TextureKind.Auto;
            Alpha = TextureAlpha.Auto;
            Sampling = TextureSampling.Auto;
            Compression = TextureCompressionKind.Auto;
            WrapU = TextureWrapKind.Clamp;
            WrapV = TextureWrapKind.Clamp;
        }

        /// <summary> Every member at once, in declaration order. </summary>
        public TextureResource(TextureResourceId textureResourceId, Vector4Value textureResourceUV,
            TextureKind kind, TextureAlpha alpha, TextureSampling sampling,
            TextureCompressionKind compression, TextureWrapKind wrapU, TextureWrapKind wrapV,
            List<ResourceKey> sources) : base(sources)
        {
            TextureResourceId = textureResourceId;
            TextureResourceUV = textureResourceUV;
            Kind = kind;
            Alpha = alpha;
            Sampling = sampling;
            Compression = compression;
            WrapU = wrapU;
            WrapV = wrapV;
        }
    }
}