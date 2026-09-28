using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Statistics
{
    // A COMPLETION IS EVERY STEP ACTUALLY PASSED, never "the tutorial was dismissed" - that is what
    // UserSettings.TutorialCompleted also records, since declining the offer sets it too. This is the
    // fact an achievement can be built on: the player did it. Kept as a group of its own rather than
    // three members on ProfileStatistics because the tutorial is an activity, not an attribute of the
    // player, and a later one (a second tutorial, a practice mode) lands beside it.

    /// <summary> What the player has done in the sandbox's tutorial. </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class TutorialStatistics : IModel<TutorialStatistics>
    {
        /// <summary> How many times every step of the tutorial was passed. </summary>
        [RuleMinValue(StatisticsRules.MinCount)]
        [JsonProperty(Names.Completions)]
        public int Completions { get; set; }

        /// <summary> When the tutorial was first completed; the default instant if it never was. </summary>
        [JsonProperty(Names.FirstCompletedUtc)]
        public DateTime FirstCompletedUtc { get; set; }

        /// <summary> When it was last completed. </summary>
        [JsonProperty(Names.LastCompletedUtc)]
        public DateTime LastCompletedUtc { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public TutorialStatistics()
        {
            Completions = 0;
            FirstCompletedUtc = default;
            LastCompletedUtc = default;
        }
    }
}
