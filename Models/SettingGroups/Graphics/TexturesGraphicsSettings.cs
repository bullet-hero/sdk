using System;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.Graphics
{
    // The device's half of how a level's images are loaded. The author's half is one field on the
    // image itself (TextureResource.Kind), and it says only WHAT the picture is - the split is the
    // whole point: a level has to play the same everywhere, so the author may not author the
    // device's memory budget, and the player may not be asked what a picture depicts. The two meet
    // in Core's TextureLoadPlanner, which is the only place that turns a (kind, settings) pair into
    // a format.
    //
    // Every field defaults to Auto and Auto resolves per platform, so a player who never opens this
    // group runs the right settings for their device. None of the three is a BaseGraphicsSettings
    // Render switch, for the same reason AntiAliasingGraphicsSettings is not: "do not render
    // textures" is not a state this game has.
    //
    // These are read when a level's resources LOAD. Changing one mid-level does nothing until the
    // next load, which is why nothing here is pushed by SettingsApplier the way anti-aliasing is.

    /// <summary>
    /// How this device turns a level's image files into GPU textures - whether it compresses them,
    /// how large it lets them be, and whether it builds mip-maps.
    /// </summary>
    [RuleContainer]
    public class TexturesGraphicsSettings : IModel<TexturesGraphicsSettings>,
        IMoveable<TexturesGraphicsSettings>
    {
        /// <summary> Whether images are packed into a GPU-compressed format. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.Compression)]
        public TextureCompressionMode Compression { get; set; }

        /// <summary> The largest side an image may occupy in memory here. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.SizeLimit)]
        public TextureSizeLimit SizeLimit { get; set; }

        /// <summary> Whether mip-maps are built for images. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.Mipmaps)]
        public TextureMipmapMode Mipmaps { get; set; }

        public TexturesGraphicsSettings()
        {
            Compression = TextureCompressionMode.Auto;
            SizeLimit = TextureSizeLimit.Auto;
            Mipmaps = TextureMipmapMode.Auto;
        }
        public TexturesGraphicsSettings(TextureCompressionMode compression,
            TextureSizeLimit sizeLimit, TextureMipmapMode mipmaps)
        {
            Compression = compression;
            SizeLimit = sizeLimit;
            Mipmaps = mipmaps;
        }
        public void Reset()
        {
            Compression = TextureCompressionMode.Auto;
            SizeLimit = TextureSizeLimit.Auto;
            Mipmaps = TextureMipmapMode.Auto;
        }

        public object Clone() => Copy();
        public TexturesGraphicsSettings Copy() => new(Compression, SizeLimit, Mipmaps);

        public void Pull(TexturesGraphicsSettings source)
        {
            Compression = source.Compression;
            SizeLimit = source.SizeLimit;
            Mipmaps = source.Mipmaps;
        }

        public void Update(TexturesGraphicsSettings src)
        {
            Compression = src.Compression;
            SizeLimit = src.SizeLimit;
            Mipmaps = src.Mipmaps;
        }

        public override bool Equals(object obj) => obj is TexturesGraphicsSettings value && Equals(value);
        public override int GetHashCode() => HashCode.Combine((int)Compression, (int)SizeLimit, (int)Mipmaps);

        public bool Equals(TexturesGraphicsSettings other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Compression == other.Compression
                   && SizeLimit == other.SizeLimit
                   && Mipmaps == other.Mipmaps;
        }
    }
}
