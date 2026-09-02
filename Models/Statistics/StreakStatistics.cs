using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Statistics
{
    // THE ONLY GROUP THAT REMEMBERS AN ORDER. Everything else here is a sum, and a sum cannot say
    // whether the last five runs were finished or abandoned - which is the thing a player actually
    // feels. The streak is the cheapest possible record of that: one number that goes up on a clear
    // and back to zero on anything else.
    //
    // MostPlayedLevelId is kept beside its own count, so it can be maintained by comparison at the
    // moment a level records an attempt. The alternative - deriving it - means opening every file in
    // stats/ to answer one line on one screen.
    //
    // A LevelId here may name a level that no longer exists. That is deliberate and needs no repair:
    // the per-level file outlives the level, carries the name it had, and the screen shows that.

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
