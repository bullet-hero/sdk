using System;
using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Profile;
using Newtonsoft.Json;

namespace BH.SDK.Versions.V2
{
    // ReSharper disable once InconsistentNaming

    // The shape did not change at generation 3 - ProfileCategory gained Skins, and an archive this
    // generation wrote can never carry it. Categories stays typed as today's enum. Keys are literal.

    /// <summary> Generation 2 of the profile-archive manifest - the shape gv 1.2.0 shipped, before the
    /// Skins category. A frozen snapshot - never edit it to match today's shape. </summary>
    [ModelGeneration(ModelDomains.ProfileManifest, ModelGenerations.V2_SimplifyEntrance)]
    [GenerateModel]
    public sealed partial class ProfileManifestV2 : IModel<ProfileManifestV2>
    {
        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("created_utc")]
        public DateTime CreatedUtc { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("game_version")]
        public string GameVersion { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("model_generation")]
        public int ModelGeneration { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("platform")]
        public string Platform { get; set; }

        /// <summary> Unchanged in shape; never carries <c>Skins</c>. </summary>
        [JsonProperty("categories")]
        public ProfileCategory Categories { get; set; }

        /// <summary> Unchanged at this bump. </summary>
        [JsonProperty("levels")]
        public List<ProfileLevelEntry> Levels { get; set; }

        /// <summary> Every member at the value generation 2 would have read into it. </summary>
        public ProfileManifestV2()
        {
            CreatedUtc = default;
            GameVersion = string.Empty;
            ModelGeneration = ModelGenerations.Invalid;
            Platform = string.Empty;
            Categories = ProfileCategory.None;
            Levels = new List<ProfileLevelEntry>();
        }
    }
}
