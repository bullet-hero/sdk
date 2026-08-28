using System;
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
    public class ScreenTimeStatistics : IModel<ScreenTimeStatistics>
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

        public ScreenTimeStatistics() => Reset();

        public void Reset()
        {
            MenuSeconds = 0.0;
            GameSeconds = 0.0;
            EditorSeconds = 0.0;
            LoadingSeconds = 0.0;
        }

        public object Clone() => Copy();

        public ScreenTimeStatistics Copy()
        {
            var copy = new ScreenTimeStatistics();
            copy.Update(this);
            return copy;
        }

        public void Update(ScreenTimeStatistics src)
        {
            MenuSeconds = src.MenuSeconds;
            GameSeconds = src.GameSeconds;
            EditorSeconds = src.EditorSeconds;
            LoadingSeconds = src.LoadingSeconds;
        }

        public void Pull(ScreenTimeStatistics source) => Update(source);

        public override bool Equals(object obj) => obj is ScreenTimeStatistics value && Equals(value);

        public bool Equals(ScreenTimeStatistics other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return MenuSeconds.Equals(other.MenuSeconds)
                   && GameSeconds.Equals(other.GameSeconds)
                   && EditorSeconds.Equals(other.EditorSeconds)
                   && LoadingSeconds.Equals(other.LoadingSeconds);
        }

        public override int GetHashCode() =>
            HashCode.Combine(MenuSeconds, GameSeconds, EditorSeconds, LoadingSeconds);
    }
}
