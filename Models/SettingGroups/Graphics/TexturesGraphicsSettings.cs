using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.Graphics
{
    // The device's half of image loading; the author's half is TextureResource's six fields. They meet in
    // Core's TextureLoadPlanner. Everything defaults to Auto and is read at LOAD. Docs/SETTING_GROUPS.md

    /// <summary>
    /// How this device turns a level's image files into GPU textures - whether it compresses them
    /// and how hard, how large it lets them be, whether it builds mip-maps, and how it samples them.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class TexturesGraphicsSettings : IModel<TexturesGraphicsSettings>,
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

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public TexturesGraphicsSettings()
        {
            Compression = TextureCompressionMode.Auto;
            SizeLimit = TextureSizeLimit.Auto;
            Mipmaps = TextureMipmapMode.Auto;
            Filtering = TextureFilterMode.Auto;
            CompressionQuality = TextureCompressionQuality.Auto;
        }

        /// <summary> Every member at once, in declaration order. </summary>
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
    }
}