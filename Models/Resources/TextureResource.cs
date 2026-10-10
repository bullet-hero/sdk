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

        // The per-resource default mapping: an object with no UV keys samples THIS, any key overrides it
        // completely (FrameMath.GetGlobalUV falls back on an empty slice). Bounded like UVKey's halves.

        /// <summary> Region of the source image this resource actually is, as tiling+offset. Applies
        /// to the resource itself, unlike UVKey which animates a single object's mapping. </summary>
        [RuleNotNull, RuleIVector4InRange(ValueRules.MinUv, ValueRules.MaxUv)]
        [JsonProperty(Names.TextureResourceUV)]
        public Vector4Value TextureResourceUV { get; set; }

        // Six independent axes of what is true of the PICTURE, never a format or a memory budget; the
        // player's settings decide the device half (Core's TextureLoadPlanner). Models/Resources/CLAUDE.md

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