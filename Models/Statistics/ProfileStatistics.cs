using System;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Statistics
{
    // FirstPlayedUtc rather than a "created" timestamp, and the difference is not pedantry: the file
    // is created by the first thing that needs to write anything, which may be the editor or a
    // settings change, while what a player means by "since" is the first time they played.

    /// <summary> Who is playing, in the only sense a local file can answer: since when, and how much. </summary>
    [RuleContainer]
    public class ProfileStatistics : IModel<ProfileStatistics>
    {
        /// <summary> When a level was first played on this device. </summary>
        [JsonProperty(Names.FirstPlayedUtc)]
        public DateTime FirstPlayedUtc { get; set; }

        /// <summary> When one was last played. </summary>
        [JsonProperty(Names.LastPlayedUtc)]
        public DateTime LastPlayedUtc { get; set; }

        /// <summary> How many times the game was started. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.AppLaunches)]
        public int AppLaunches { get; set; }

        /// <summary> Real seconds the game has been open, every screen included. </summary>
        [RuleInRange(StatisticsRules.MinSeconds, StatisticsRules.MaxSeconds)]
        [JsonProperty(Names.AppSeconds)]
        public double TotalAppSeconds { get; set; }

        public ProfileStatistics() => Reset();

        public void Reset()
        {
            FirstPlayedUtc = default;
            LastPlayedUtc = default;
            AppLaunches = 0;
            TotalAppSeconds = 0.0;
        }

        public object Clone() => Copy();

        public ProfileStatistics Copy()
        {
            var copy = new ProfileStatistics();
            copy.Update(this);
            return copy;
        }

        public void Update(ProfileStatistics src)
        {
            FirstPlayedUtc = src.FirstPlayedUtc;
            LastPlayedUtc = src.LastPlayedUtc;
            AppLaunches = src.AppLaunches;
            TotalAppSeconds = src.TotalAppSeconds;
        }

        public void Pull(ProfileStatistics source) => Update(source);

        public override bool Equals(object obj) => obj is ProfileStatistics value && Equals(value);

        public bool Equals(ProfileStatistics other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return FirstPlayedUtc.Equals(other.FirstPlayedUtc)
                   && LastPlayedUtc.Equals(other.LastPlayedUtc)
                   && AppLaunches == other.AppLaunches
                   && TotalAppSeconds.Equals(other.TotalAppSeconds);
        }

        public override int GetHashCode() =>
            HashCode.Combine(FirstPlayedUtc, LastPlayedUtc, AppLaunches, TotalAppSeconds);
    }
}
