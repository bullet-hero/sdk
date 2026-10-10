using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Statistics
{
    // The sum over every level, kept to avoid opening each file; the Distinct* members are COUNTERS, not
    // id sets. Docs/STATISTICS.md

    /// <summary> Every counter summed across every level. </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class TotalsStatistics : IModel<TotalsStatistics>
    {
        /// <summary> Runs started across every level. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.Attempts)]
        public int TotalAttempts { get; set; }

        /// <summary> Runs finished. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.Clears)]
        public int TotalClears { get; set; }

        /// <summary> Runs ended by running out of lives. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.Deaths)]
        public int TotalDeaths { get; set; }

        /// <summary> Individual hits taken, whether or not they ended a run. </summary>
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

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
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
