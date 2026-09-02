using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Statistics
{
    // THE SUM OVER EVERY LEVEL, kept here rather than computed by walking stats/ on demand: the
    // profile screen would otherwise have to open every per-level file the player has ever made,
    // which is the one thing the per-level split was chosen to avoid.
    //
    // DistinctLevelsPlayed and DistinctLevelsCleared are COUNTERS, not sets of ids. A set would grow
    // without bound inside a file read on every launch, and neither number is ever asked "which
    // ones" - that question is answered by listing stats/ itself. The first is incremented when a
    // level file is created, the second when a level first records a clear.

    /// <summary> Every counter summed across every level. </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class TotalsStatistics : IModel<TotalsStatistics>
    {
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.Attempts)]
        public int TotalAttempts { get; set; }

        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.Clears)]
        public int TotalClears { get; set; }

        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.Deaths)]
        public int TotalDeaths { get; set; }

        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.Hits)]
        public int TotalHits { get; set; }

        /// <summary> How many different levels have ever been played. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.DistinctLevelsPlayed)]
        public int DistinctLevelsPlayed { get; set; }

        /// <summary> How many different levels have ever been finished. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.DistinctLevelsCleared)]
        public int DistinctLevelsCleared { get; set; }

        /// <summary> Frames of level simulated. A long, because at 60 fps an int overflows after
        /// about 414 days of play, which is a number this game hopes to reach. </summary>
        [JsonProperty(Names.FramesSimulated)]
        public long TotalFramesSimulated { get; set; }

        public TotalsStatistics()
        {
            TotalAttempts = 0;
            TotalClears = 0;
            TotalDeaths = 0;
            TotalHits = 0;
            DistinctLevelsPlayed = 0;
            DistinctLevelsCleared = 0;
            TotalFramesSimulated = 0L;
        }
    }
}
