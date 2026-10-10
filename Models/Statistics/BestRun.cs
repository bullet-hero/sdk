using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Statistics
{
    // The best run under one RunProfile and it carries none of its conditions (LivesLeft is not one of them).
    // LevelVersion is deliberately not in the key. Docs/STATISTICS.md

    /// <summary> The best run recorded under one <see cref="RunProfile"/>. </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class BestRun : IModel<BestRun>
    {
        /// <summary> How far the run got, 0 to 1. </summary>
        [RuleInRange(0f, 1f)]
        [JsonProperty(Names.Progress)]
        public float Progress { get; set; }

        /// <summary> The furthest frame reached - the exact form of <see cref="Progress"/>, kept
        /// because the level's own length can change under a record.
        /// <see cref="FrameRules.NoFrame"/> when nothing was reached, which is why the floor here is
        /// zero rather than <see cref="FrameRules.MinFrame"/>. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.Frame)]
        public int Frame { get; set; }

        /// <summary> Hits taken over the whole run. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.Hits)]
        public int Hits { get; set; }

        /// <summary> Dashes spent over the whole run. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.Dashes)]
        public int Dashes { get; set; }

        /// <summary> Lives remaining when the run ended. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.LivesLeft)]
        public int LivesLeft { get; set; }

        /// <summary> The seed the run resolved to, so it can be replayed. </summary>
        [JsonProperty(Names.Seed)]
        public int Seed { get; set; }

        /// <summary> The level's own version when the record was set. </summary>
        [RuleNotNull(1, 0)]
        [JsonProperty(Names.Version)]
        public Version LevelVersion { get; set; }

        /// <summary> When it was set. Always UTC - see LevelStatistics' header. </summary>
        [JsonProperty(Names.TimeUtc)]
        public DateTime TimeUtc { get; set; }

        // Public and parameterless because RuleContainerAnalyzer requires it of every
        // [RuleContainer] class: several Fix paths construct property types through
        // Activator.CreateInstance.

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public BestRun()
        {
            Progress = 0f;
            Frame = FrameRules.NoFrame;
            Hits = 0;
            Dashes = 0;
            LivesLeft = 0;
            Seed = 0;
            LevelVersion = new Version(1, 0);
            TimeUtc = default;
        }

        /// <summary> Every member at once, in declaration order. </summary>
        public BestRun(float progress, int frame, int hits, int dashes, int livesLeft,
            int seed, Version levelVersion, DateTime timeUtc)
        {
            Progress = progress;
            Frame = frame;
            Hits = hits;
            Dashes = dashes;
            LivesLeft = livesLeft;
            Seed = seed;
            LevelVersion = levelVersion;
            TimeUtc = timeUtc;
        }

        // No nested model of its own, so Pull and Update coincide - the contract agreeing rather
        // than duplication to collapse. Version is immutable, so sharing the instance is safe on
        // both paths.
    }
}