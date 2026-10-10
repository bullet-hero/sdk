using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Statistics
{
    // Carries its own key (the dictionary serializes as an array). Keyed by the checkpoint's FRAME, never
    // an index: an index shifts on insert. Docs/STATISTICS.md

    /// <summary> How many deaths happened while one checkpoint was the last one reached. </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class CheckpointDeaths : IModel<CheckpointDeaths>
    {
        /// <summary> Frame of the checkpoint this counts for - the dictionary's key. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.Frame)]
        public int Frame { get; set; }

        /// <summary> Deaths recorded past that checkpoint and before the next one. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.Deaths)]
        public int Deaths { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public CheckpointDeaths()
        {
            Frame = FrameRules.NoFrame;
            Deaths = 0;
        }

        /// <summary> Built from its frame and deaths. </summary>
        public CheckpointDeaths(int frame, int deaths)
        {
            Frame = frame;
            Deaths = deaths;
        }
    }
}
