using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using BH.SDK.Versions;
using Newtonsoft.Json;

namespace BH.SDK.Models
{
    // A new domain, not a bump: no existing model refers to it. Tombstones are never pruned - a level id
    // and a date cost a few dozen bytes, and a pruned one would let an offline device resurrect the level.

    /// <summary> A cloud save's index of levels (<c>cloud.json</c>): every level synced through it,
    /// deleted ones included. </summary>
    [RuleContainer]
    [ModelGeneration(ModelDomains.CloudManifest, ModelGenerations.V2_SimplifyEntrance)]
    [GenerateModel]
    public sealed partial class CloudManifest : IModel<CloudManifest>
    {
        /// <summary> Every level the cloud holds or has held. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Levels)]
        public List<CloudLevelEntry> Levels { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public CloudManifest()
        {
            Levels = new List<CloudLevelEntry>();
        }
    }
}
