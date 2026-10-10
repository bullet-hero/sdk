using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Statistics
{
    // Four numbers; they do NOT sum to TotalAppSeconds, which is measured on its own. Docs/STATISTICS.md

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

        /// <summary> The seconds recorded for one screen. </summary>
        public double Get(ScreenTimeKind kind) => kind switch
        {
            ScreenTimeKind.Menu => MenuSeconds,
            ScreenTimeKind.Game => GameSeconds,
            ScreenTimeKind.Editor => EditorSeconds,
            ScreenTimeKind.Loading => LoadingSeconds,
            _ => 0.0,
        };

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public ScreenTimeStatistics()
        {
            MenuSeconds = 0.0;
            GameSeconds = 0.0;
            EditorSeconds = 0.0;
            LoadingSeconds = 0.0;
        }
    }
}
