using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Statistics
{
    // WHERE THE TIME ACTUALLY WENT. Four numbers rather than one, because "I have 40 hours in this
    // game" answers a different question from "I have 30 of them in the editor" - and for a game
    // that is also an authoring tool, the second is the more interesting one.
    //
    // Loading is separated for a reason that is not vanity: it is the only one of the four the game
    // can hope to shrink, so it is the one worth being able to measure across a release.
    //
    // These do NOT sum to ProfileStatistics.TotalAppSeconds and are not meant to: a screen being
    // torn down belongs to nobody, and time spent in a menu over a paused level is charged once. The
    // total is measured on its own rather than derived, so neither number can drift into being the
    // other minus a rounding error.

    /// <summary> Real seconds spent on each kind of screen. </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class ScreenTimeStatistics : IModel<ScreenTimeStatistics>
    {
        /// <summary> Time in the menu, its own background arena included. </summary>
        [RuleInRange(StatisticsRules.MinSeconds, StatisticsRules.MaxSeconds)]
        [JsonProperty(Names.MenuSeconds)]
        public double MenuSeconds { get; set; }

        /// <summary> Time on the game screen, paused or not. </summary>
        [RuleInRange(StatisticsRules.MinSeconds, StatisticsRules.MaxSeconds)]
        [JsonProperty(Names.GameSeconds)]
        public double GameSeconds { get; set; }

        /// <summary> Time in the level editor. </summary>
        [RuleInRange(StatisticsRules.MinSeconds, StatisticsRules.MaxSeconds)]
        [JsonProperty(Names.EditorSeconds)]
        public double EditorSeconds { get; set; }

        /// <summary> Time spent waiting on a loading screen. </summary>
        [RuleInRange(StatisticsRules.MinSeconds, StatisticsRules.MaxSeconds)]
        [JsonProperty(Names.LoadingSeconds)]
        public double LoadingSeconds { get; set; }

        /// <summary> Charges seconds to one kind of screen. An unknown kind is ignored rather than
        /// folded into the menu, since a wrong attribution is worse than a missing one. </summary>
        public void Add(ScreenTimeKind kind, double seconds)
        {
            switch (kind)
            {
                case ScreenTimeKind.Menu: MenuSeconds += seconds; break;
                case ScreenTimeKind.Game: GameSeconds += seconds; break;
                case ScreenTimeKind.Editor: EditorSeconds += seconds; break;
                case ScreenTimeKind.Loading: LoadingSeconds += seconds; break;
            }
        }

        public double Get(ScreenTimeKind kind) => kind switch
        {
            ScreenTimeKind.Menu => MenuSeconds,
            ScreenTimeKind.Game => GameSeconds,
            ScreenTimeKind.Editor => EditorSeconds,
            ScreenTimeKind.Loading => LoadingSeconds,
            _ => 0.0,
        };

        public ScreenTimeStatistics()
        {
            MenuSeconds = 0.0;
            GameSeconds = 0.0;
            EditorSeconds = 0.0;
            LoadingSeconds = 0.0;
        }
    }
}
