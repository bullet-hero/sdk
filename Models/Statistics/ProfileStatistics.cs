using System;
using BH.SDK.Models.Attributes;
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
    [GenerateModel]
    public sealed partial class ProfileStatistics : IModel<ProfileStatistics>
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

        public ProfileStatistics()
        {
            FirstPlayedUtc = default;
            LastPlayedUtc = default;
            AppLaunches = 0;
            TotalAppSeconds = 0.0;
        }
    }
}
