using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives;
using Newtonsoft.Json;

namespace BH.SDK.Models.Profile
{
    // THE TIMESTAMP HAS TO TRAVEL HERE because the archive cannot carry it: every zip entry is pinned
    // to 1980 so two packs of one profile are the same bytes (ArchivePolicy), and LevelMeta has no
    // edit time of its own. A merge deciding "which copy of this level is newer" reads this number
    // for the incoming side, and an import writes it back onto the level document it places - or
    // every imported level would look newest to the next merge and to the browser's sort.

    /// <summary> One level a profile archive carries: its identity, its folder under
    /// <c>levels/</c>, and when its level document was last written on the exporting device. </summary>
    [GenerateModel]
    public sealed partial class ProfileLevelEntry : IModel<ProfileLevelEntry>
    {
        /// <summary> The level's stable identity - what a merge matches on. </summary>
        [JsonProperty(Names.LevelId)]
        public LevelId LevelId { get; set; }

        /// <summary> The folder name under <c>levels/</c> in the archive. </summary>
        [JsonProperty(Names.Folder)]
        public string Folder { get; set; }

        /// <summary> Last write time of the level document on the exporting device, UTC. </summary>
        [JsonProperty(Names.ModifiedUtc)]
        public DateTime ModifiedUtc { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public ProfileLevelEntry()
        {
            LevelId = LevelId.Null;
            Folder = string.Empty;
            ModifiedUtc = default;
        }

        /// <summary> Built from its three values. </summary>
        public ProfileLevelEntry(LevelId levelId, string folder, DateTime modifiedUtc)
        {
            LevelId = levelId;
            Folder = folder;
            ModifiedUtc = modifiedUtc;
        }
    }
}
