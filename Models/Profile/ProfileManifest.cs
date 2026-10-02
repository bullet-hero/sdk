using System;
using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using BH.SDK.Versions;
using Newtonsoft.Json;

namespace BH.SDK.Models.Profile
{
    // WHAT A PROFILE ARCHIVE IS, besides the player's own files under their own names. It is the first
    // entry of the zip, so a reader learns what it holds - and whether this build may read it - from
    // the central directory and one small document, before anything is unpacked.
    //
    // CATEGORIES ARE STORED, NOT DERIVED FROM THE ENTRIES. "Levels were exported and there were none"
    // and "levels were not exported" are different instructions to a Replace import: the first empties
    // levels/, the second must not touch it. An entry listing cannot tell them apart.
    //
    // A NEW DOMAIN, NOT A BUMP. Nothing on any disk has this shape, and no existing model refers to it.

    /// <summary> A profile archive's manifest (<c>profile.json</c>): when and by what it was written,
    /// which categories it carries, and the levels with the timestamps a merge compares. </summary>
    [RuleContainer]
    [ModelGeneration(ModelDomains.ProfileManifest, ModelGenerations.V2_SimplifyEntrance)]
    [GenerateModel]
    public sealed partial class ProfileManifest : IModel<ProfileManifest>
    {
        /// <summary> When the archive was written, UTC. </summary>
        [JsonProperty(Names.CreatedUtc)]
        public DateTime CreatedUtc { get; set; }

        /// <summary> The exporting build's game version - only ever shown, in "update the game". </summary>
        [RuleNotNull]
        [JsonProperty(Names.GameVersion)]
        public string GameVersion { get; set; }

        /// <summary> The exporting build's <see cref="ModelGenerations.Current"/>. An archive above this
        /// build's own is refused whole. </summary>
        [JsonProperty(Names.ModelGeneration)]
        public int ModelGeneration { get; set; }

        /// <summary> The exporting platform's name, which the host maps to a platform class. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Platform)]
        public string Platform { get; set; }

        /// <summary> The categories the archive was asked to carry, empty ones included. </summary>
        [JsonProperty(Names.Categories)]
        public ProfileCategory Categories { get; set; }

        /// <summary> Every level under <c>levels/</c>, with the time a merge compares. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Levels)]
        public List<ProfileLevelEntry> Levels { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public ProfileManifest()
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
