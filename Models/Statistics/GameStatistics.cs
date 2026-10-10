using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using BH.SDK.Versions;
using Newtonsoft.Json;

namespace BH.SDK.Models.Statistics
{
    // The player across everything (stats/statistics.json), its own document so one screen does not open
    // every file in stats/. One envelope per FILE, groups carry no generation. Docs/STATISTICS.md

    /// <summary> Everything one player has done, across every level and every screen. </summary>
    [RuleContainer]
    [ModelGeneration(ModelDomains.GameStatistics, ModelGenerations.V2_SimplifyEntrance)]
    [GenerateModel]
    public sealed partial class GameStatistics : IModel<GameStatistics>
    {
        /// <summary> Since when, and how much. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Profile)]
        public ProfileStatistics Profile { get; set; }

        /// <summary> Where the time went, per kind of screen. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Screens)]
        public ScreenTimeStatistics Screens { get; set; }

        /// <summary> Every per-level counter, summed. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Totals)]
        public TotalsStatistics Totals { get; set; }

        /// <summary> The parts of a history a running total cannot express. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Streaks)]
        public StreakStatistics Streaks { get; set; }

        /// <summary> What the avatar itself has done. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Avatar)]
        public AvatarStatistics Avatar { get; set; }

        /// <summary> What has been authored on this device. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Editor)]
        public EditorTotalsStatistics Editor { get; set; }

        /// <summary> Which control device actually steers. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Devices)]
        public DeviceTimeStatistics Devices { get; set; }

        /// <summary> What was done in the sandbox's tutorial. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Tutorial)]
        public TutorialStatistics Tutorial { get; set; }

        // TODO post-alpha feature: achievements. When they arrive they belong here as a ninth group, keyed by a stable
        // achievement id. Deliberately not scaffolded now: an empty aggregate cannot be told apart
        // from "this build has no achievements", and every reader would have to handle both anyway.

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public GameStatistics()
        {
            Profile = new ProfileStatistics();
            Screens = new ScreenTimeStatistics();
            Totals = new TotalsStatistics();
            Streaks = new StreakStatistics();
            Avatar = new AvatarStatistics();
            Editor = new EditorTotalsStatistics();
            Devices = new DeviceTimeStatistics();
            Tutorial = new TutorialStatistics();
        }

        // Every group instance is kept, unlike Update: the app holds a reference to each (IMoveable).
    }
}