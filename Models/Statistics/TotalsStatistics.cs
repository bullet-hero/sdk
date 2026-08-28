using System;
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
    public class TotalsStatistics : IModel<TotalsStatistics>
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

        public TotalsStatistics() => Reset();

        public void Reset()
        {
            TotalAttempts = 0;
            TotalClears = 0;
            TotalDeaths = 0;
            TotalHits = 0;
            DistinctLevelsPlayed = 0;
            DistinctLevelsCleared = 0;
            TotalFramesSimulated = 0L;
        }

        public object Clone() => Copy();

        public TotalsStatistics Copy()
        {
            var copy = new TotalsStatistics();
            copy.Update(this);
            return copy;
        }

        public void Update(TotalsStatistics src)
        {
            TotalAttempts = src.TotalAttempts;
            TotalClears = src.TotalClears;
            TotalDeaths = src.TotalDeaths;
            TotalHits = src.TotalHits;
            DistinctLevelsPlayed = src.DistinctLevelsPlayed;
            DistinctLevelsCleared = src.DistinctLevelsCleared;
            TotalFramesSimulated = src.TotalFramesSimulated;
        }

        public void Pull(TotalsStatistics source) => Update(source);

        public override bool Equals(object obj) => obj is TotalsStatistics value && Equals(value);

        public bool Equals(TotalsStatistics other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return TotalAttempts == other.TotalAttempts
                   && TotalClears == other.TotalClears
                   && TotalDeaths == other.TotalDeaths
                   && TotalHits == other.TotalHits
                   && DistinctLevelsPlayed == other.DistinctLevelsPlayed
                   && DistinctLevelsCleared == other.DistinctLevelsCleared
                   && TotalFramesSimulated == other.TotalFramesSimulated;
        }

        public override int GetHashCode() =>
            HashCode.Combine(TotalAttempts, TotalClears, TotalDeaths, TotalHits,
                DistinctLevelsPlayed, DistinctLevelsCleared, TotalFramesSimulated);
    }
}
