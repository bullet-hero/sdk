using System;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Statistics
{
    // Carries its own key, which is what lets the dictionary holding these serialize as a plain
    // array with the key dropped (DictionaryCheckpointDeathsConverter) rather than as the {K,V}
    // pair form a bare int-to-int map would need. Same shape as CachedFontText and Modification.
    //
    // KEYED BY THE CHECKPOINT'S FRAME, never by an index into the level's checkpoint list: an index
    // shifts the moment a checkpoint is inserted before it, silently reassigning every count to a
    // different passage of the level. A frame goes stale honestly instead - move the checkpoint and
    // the entry is simply orphaned, which is the price the level-global keyframe tracks already pay
    // for the same reason.

    /// <summary> How many deaths happened while one checkpoint was the last one reached. </summary>
    [RuleContainer]
    public class CheckpointDeaths : IModel<CheckpointDeaths>
    {
        /// <summary> Frame of the checkpoint this counts for - the dictionary's key. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.Frame)]
        public int Frame { get; set; }

        /// <summary> Deaths recorded past that checkpoint and before the next one. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.Deaths)]
        public int Deaths { get; set; }

        public CheckpointDeaths()
        {
            Reset();
        }

        public CheckpointDeaths(int frame, int deaths)
        {
            Frame = frame;
            Deaths = deaths;
        }

        public void Reset()
        {
            Frame = 0;
            Deaths = 0;
        }

        public object Clone() => Copy();
        public CheckpointDeaths Copy() => new(Frame, Deaths);

        public void Update(CheckpointDeaths src)
        {
            Frame = src.Frame;
            Deaths = src.Deaths;
        }

        public void Pull(CheckpointDeaths source) => Update(source);

        public override bool Equals(object obj) => obj is CheckpointDeaths value && Equals(value);

        public bool Equals(CheckpointDeaths other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Frame == other.Frame && Deaths == other.Deaths;
        }

        public override int GetHashCode() => HashCode.Combine(Frame, Deaths);
    }
}
