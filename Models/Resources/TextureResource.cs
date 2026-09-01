using System;
using System.Collections.Generic;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Resources
{
    /// <summary>
    /// An image the level brings with it. The only Resource subtype with extra data of its own -
    /// a sub-rect, so one shipped atlas can back many different sprites.
    /// </summary>
    [RuleContainer]
    public class TextureResource : Resource, IModel<TextureResource>
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

        // THREE FIELDS, THREE INDEPENDENT AXES, and none of them is a format, a size or a switch.
        // A level has to play the same on every device, so the author says what is true of the
        // PICTURE and the player's own settings (UserSettings.Graphics.Textures) decide what this
        // device does about it; Core's TextureLoadPlanner is the only place the two meet.
        //
        // Kind names what the picture is. Alpha answers the one question about it that a device can
        // read the file and still not know (see TextureAlpha's own header). Wrap says what lies
        // outside its edges, which is composition rather than budget and therefore has no player
        // half at all. They are separate because they genuinely vary independently - an opaque
        // pixel-art tile that repeats is three answers, not one.
        //
        // All three are additive with a zero default, so none needed a migration and LevelResources
        // stays at (1, 0): a level written before any of them reads back as Auto/Auto/Clamp, which
        // is exactly the behaviour it already had.

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

        /// <summary> What this image does past its own edges, which is what makes
        /// <see cref="TextureResourceUV"/>'s tiling half mean anything. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.Wrap)]
        public TextureWrapKind Wrap { get; set; }

        public override ResourceType Type => ResourceType.Texture;

        public TextureResource()
        {
            TextureResourceId = TextureResourceId.Null;
            TextureResourceUV = new Vector4Value(ValueRules.DefaultUvX,
                ValueRules.DefaultUvY, ValueRules.DefaultUvZ, ValueRules.DefaultUvW);
            Kind = TextureKind.Auto;
            Alpha = TextureAlpha.Auto;
            Wrap = TextureWrapKind.Clamp;
        }

        public TextureResource(TextureResourceId textureResourceId, List<ResourceKey> sources) : base(sources)
        {
            TextureResourceId = textureResourceId;
            TextureResourceUV = new Vector4Value(ValueRules.DefaultUvX,
                ValueRules.DefaultUvY, ValueRules.DefaultUvZ, ValueRules.DefaultUvW);
            Kind = TextureKind.Auto;
            Alpha = TextureAlpha.Auto;
            Wrap = TextureWrapKind.Clamp;
        }

        public TextureResource(TextureResourceId textureResourceId, Vector4Value textureResourceUV,
            List<ResourceKey> sources) : base(sources)
        {
            TextureResourceId = textureResourceId;
            TextureResourceUV = textureResourceUV;
            Kind = TextureKind.Auto;
            Alpha = TextureAlpha.Auto;
            Wrap = TextureWrapKind.Clamp;
        }

        public TextureResource(TextureResourceId textureResourceId, Vector4Value textureResourceUV,
            TextureKind kind, TextureAlpha alpha, TextureWrapKind wrap, List<ResourceKey> sources)
            : base(sources)
        {
            TextureResourceId = textureResourceId;
            TextureResourceUV = textureResourceUV;
            Kind = kind;
            Alpha = alpha;
            Wrap = wrap;
        }

        public override void Reset()
        {
            base.Reset();
            TextureResourceId = TextureResourceId.Null;
            TextureResourceUV = new Vector4Value(ValueRules.DefaultUvX,
                ValueRules.DefaultUvY, ValueRules.DefaultUvZ, ValueRules.DefaultUvW);
            Kind = TextureKind.Auto;
            Alpha = TextureAlpha.Auto;
            Wrap = TextureWrapKind.Clamp;
        }

        public override object Clone() => CopyImpl();
        public override Resource Copy() => CopyImpl();
        TextureResource ICopyable<TextureResource>.Copy() => CopyImpl();

        private TextureResource CopyImpl() =>
            new(TextureResourceId, TextureResourceUV.Copy(), Kind, Alpha, Wrap, Sources.CopyList());

        public void Update(TextureResource src)
        {
            base.Update(src);

            TextureResourceId = src.TextureResourceId;
            TextureResourceUV = src.TextureResourceUV.Copy();
            Kind = src.Kind;
            Alpha = src.Alpha;
            Wrap = src.Wrap;
        }

        public void Pull(TextureResource src)
        {
            base.Pull(src);

            TextureResourceId = src.TextureResourceId;
            TextureResourceUV.Pull(src.TextureResourceUV);
            Kind = src.Kind;
            Alpha = src.Alpha;
            Wrap = src.Wrap;
        }

        public override bool Equals(object obj) => obj is TextureResource value && Equals(value);

        public override int GetHashCode() =>
            HashCode.Combine(base.GetHashCode(), TextureResourceId, TextureResourceUV, (int)Kind,
                (int)Alpha, (int)Wrap);

        public bool Equals(TextureResource other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other)
                         && TextureResourceId.Equals(other.TextureResourceId)
                         && TextureResourceUV.Equals(other.TextureResourceUV)
                         && Kind == other.Kind
                         && Alpha == other.Alpha
                         && Wrap == other.Wrap;
            return result;
        }
    }
}