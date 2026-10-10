using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Statistics
{
    // Global only, in WORLD UNITS, from real runs only (the menu arena would count idle time).
    // Docs/STATISTICS.md

    /// <summary> What the avatar itself has done, across every level. </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class AvatarStatistics : IModel<AvatarStatistics>
    {
        /// <summary> Dashes spent. A long for the same reason the frame counter is one. </summary>
        [JsonProperty(Names.TotalDashes)]
        public long TotalDashes { get; set; }

        /// <summary> Distance travelled, in world units. </summary>
        [JsonProperty(Names.TotalDistanceMoved)]
        public double TotalDistanceMoved { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public AvatarStatistics()
        {
            TotalDashes = 0L;
            TotalDistanceMoved = 0.0;
        }
    }
}
