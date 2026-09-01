using System;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.Graphics
{
    // The device's half of how a level's images are loaded. The author's half is three fields on the
    // image itself (TextureResource's Kind/Alpha/Wrap), and they say only what is true of the
    // PICTURE - the split is the whole point: a level has to play the same everywhere, so the author
    // may not author the device's memory budget, and the player may not be asked what a picture
    // depicts. The two meet in Core's TextureLoadPlanner, which is the only place that turns an
    // (authoring, settings) pair into a format.
    //
    // Every field defaults to Auto and Auto resolves per platform, so a player who never opens this
    // group runs the right settings for their device. None of them is a BaseGraphicsSettings Render
    // switch, for the same reason AntiAliasingGraphicsSettings is not: "do not render textures" is
    // not a state this game has.
    //
    // TWO OF THESE USED TO BE DERIVED FROM THE AUTHOR'S KIND and were on the wrong side of the split:
    // the sampling filter (pixel art point-sampled, everything else smoothed) and the compression
    // encoder's effort (everything but a photo took the careful one). Encoder effort is pure load
    // TIME and filtering is how the device draws, so both are the player's. Their Auto reproduces the
    // old derivation exactly, and a kind may still RESTRICT - PixelArt forces Point however Filtering
    // is set, exactly as it already forces compression and mip-maps off.
    //
    // These are read when a level's resources LOAD. Changing one mid-level does nothing until the
    // next load, which is why nothing here is pushed by SettingsApplier the way anti-aliasing is.

    /// <summary>
    /// How this device turns a level's image files into GPU textures - whether it compresses them
    /// and how hard, how large it lets them be, whether it builds mip-maps, and how it samples them.
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

        /// <summary> How images are sampled when drawn. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.Filter)]
        public TextureFilterMode Filtering { get; set; }

        /// <summary> How much time is spent compressing an image. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.Quality)]
        public TextureCompressionQuality CompressionQuality { get; set; }

        public TexturesGraphicsSettings()
        {
            Compression = TextureCompressionMode.Auto;
            SizeLimit = TextureSizeLimit.Auto;
            Mipmaps = TextureMipmapMode.Auto;
            Filtering = TextureFilterMode.Auto;
            CompressionQuality = TextureCompressionQuality.Auto;
        }

        public TexturesGraphicsSettings(TextureCompressionMode compression,
            TextureSizeLimit sizeLimit, TextureMipmapMode mipmaps, TextureFilterMode filtering,
            TextureCompressionQuality compressionQuality)
        {
            Compression = compression;
            SizeLimit = sizeLimit;
            Mipmaps = mipmaps;
            Filtering = filtering;
            CompressionQuality = compressionQuality;
        }

        public void Reset()
        {
            Compression = TextureCompressionMode.Auto;
            SizeLimit = TextureSizeLimit.Auto;
            Mipmaps = TextureMipmapMode.Auto;
            Filtering = TextureFilterMode.Auto;
            CompressionQuality = TextureCompressionQuality.Auto;
        }

        public object Clone() => Copy();
        public TexturesGraphicsSettings Copy() =>
            new(Compression, SizeLimit, Mipmaps, Filtering, CompressionQuality);

        public void Pull(TexturesGraphicsSettings source)
        {
            Compression = source.Compression;
            SizeLimit = source.SizeLimit;
            Mipmaps = source.Mipmaps;
            Filtering = source.Filtering;
            CompressionQuality = source.CompressionQuality;
        }

        public void Update(TexturesGraphicsSettings src)
        {
            Compression = src.Compression;
            SizeLimit = src.SizeLimit;
            Mipmaps = src.Mipmaps;
            Filtering = src.Filtering;
            CompressionQuality = src.CompressionQuality;
        }

        public override bool Equals(object obj) => obj is TexturesGraphicsSettings value && Equals(value);
        public override int GetHashCode() => HashCode.Combine((int)Compression, (int)SizeLimit, (int)Mipmaps,
                (int)Filtering, (int)CompressionQuality);

        public bool Equals(TexturesGraphicsSettings other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Compression == other.Compression
                   && SizeLimit == other.SizeLimit
                   && Mipmaps == other.Mipmaps
                   && Filtering == other.Filtering
                   && CompressionQuality == other.CompressionQuality;
        }
    }
}