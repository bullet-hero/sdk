using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Statistics
{
    // The only group that remembers an ORDER. MostPlayedLevelId may name a deleted level, deliberately.
    // Docs/STATISTICS.md

    /// <summary> The parts of a history that a running total cannot express. </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class StreakStatistics : IModel<StreakStatistics>
    {
        /// <summary> Levels cleared in a row, right now. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.CurrentClearStreak)]
        public int CurrentClearStreak { get; set; }

        /// <summary> The longest such run ever. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.LongestClearStreak)]
        public int LongestClearStreak { get; set; }

        /// <summary> The level with the most attempts. </summary>
        [JsonProperty(Names.MostPlayedLevelId)]
        public LevelId MostPlayedLevelId { get; set; }

        /// <summary> That level attempt count, kept so the comparison stays a comparison. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.MostPlayedAttempts)]
        public int MostPlayedAttempts { get; set; }

        /// <summary> The level played most recently. </summary>
        [JsonProperty(Names.LastPlayedLevelId)]
        public LevelId LastPlayedLevelId { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public StreakStatistics()
        {
            CurrentClearStreak = 0;
            LongestClearStreak = 0;
            MostPlayedLevelId = LevelId.Null;
            MostPlayedAttempts = 0;
            LastPlayedLevelId = LevelId.Null;
        }

        /// <summary> Records that a level now has this many attempts, taking the crown if it leads.
        /// Comparison rather than assignment, so the same level reporting again cannot lose it. </summary>
        public void ReportAttempts(LevelId levelId, int attempts)
        {
            LastPlayedLevelId = levelId;
            if (attempts < MostPlayedAttempts) return;

            MostPlayedLevelId = levelId;
            MostPlayedAttempts = attempts;
        }

        /// <summary> Extends the streak on a clear, breaks it on anything else. </summary>
        public void ReportOutcome(bool cleared)
        {
            if (!cleared)
            {
                CurrentClearStreak = 0;
                return;
            }

            CurrentClearStreak++;
            if (CurrentClearStreak > LongestClearStreak) LongestClearStreak = CurrentClearStreak;
        }
    }
}
