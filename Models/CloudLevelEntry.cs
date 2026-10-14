using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives;
using Newtonsoft.Json;

namespace BH.SDK.Models
{
    /// <summary> One level a cloud save holds, or held: its identity, its folder under <c>levels/</c>
    /// in the cloud, when its level document was last written, and whether it was deleted. </summary>
    [GenerateModel]
    public sealed partial class CloudLevelEntry : IModel<CloudLevelEntry>
    {
        /// <summary> The level's stable identity - what a sync matches on. </summary>
        [JsonProperty(Names.LevelId)]
        public LevelId LevelId { get; set; }

        /// <summary> The folder name under <c>levels/</c> in the cloud; each device keeps its own. </summary>
        [JsonProperty(Names.Folder)]
        public string Folder { get; set; }

        /// <summary> Last write time of the level document on the device that uploaded it, UTC. </summary>
        [JsonProperty(Names.ModifiedUtc)]
        public DateTime ModifiedUtc { get; set; }

        /// <summary> A tombstone: the level was deleted, and every device deletes its copy. </summary>
        [JsonProperty(Names.Deleted)]
        public bool Deleted { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public CloudLevelEntry()
        {
            LevelId = LevelId.Null;
            Folder = string.Empty;
            ModifiedUtc = default;
            Deleted = false;
        }

        /// <summary> Built from its four values. </summary>
        public CloudLevelEntry(LevelId levelId, string folder, DateTime modifiedUtc, bool deleted)
        {
            LevelId = levelId;
            Folder = folder;
            ModifiedUtc = modifiedUtc;
            Deleted = deleted;
        }
    }
}
