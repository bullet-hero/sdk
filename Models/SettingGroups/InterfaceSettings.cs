using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups
{
    // Two free floats in level-content convention (0,0 lower left, 1,1 upper right): a hand-edited 0.25,0.9
    // is legal data; outside [0,1] would park the overlay off screen.

    /// <summary>
    /// Device-wide options for the game's own interface overlays - today the diagnostics readout that
    /// every screen can draw over itself. Nothing here travels with a level.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class InterfaceSettings : IModel<InterfaceSettings>, IMoveable<InterfaceSettings>
    {
        // False: a lost run rewinds to the last checkpoint (CheckpointService); on, the result window.

        /// <summary> Whether losing a run opens the result window instead of respawning. </summary>
        [JsonProperty(Names.OpenMenuOnLose)]
        public bool OpenMenuOnLose { get; set; }

        /// <summary> Whether the statistics overlay is drawn at all. </summary>
        [JsonProperty(Names.StatsActive)]
        public bool StatsActive { get; set; }

        // The three diagnostics blocks default off. The level flag gates an O(objects) walk, so off
        // collects nothing; the memory block was always drawn. Docs/SETTING_GROUPS.md

        /// <summary> Whether the overlay draws the per-frame buffer usage block. </summary>
        [JsonProperty(Names.StatsFrameObjects)]
        public bool StatsFrameObjects { get; set; }

        /// <summary> Whether the overlay draws the loaded level's own size block. </summary>
        [JsonProperty(Names.StatsLevelObjects)]
        public bool StatsLevelObjects { get; set; }

        /// <summary> Whether the overlay draws the process memory block. </summary>
        [JsonProperty(Names.StatsMemory)]
        public bool StatsMemory { get; set; }

        // Gates a heavier walk (every UI element); nothing in a release build, where the markers compile out.

        /// <summary> Whether the overlay draws the profiling block: pipeline stage timings and how
        /// many elements the UI panel holds. </summary>
        [JsonProperty(Names.StatsProfiling)]
        public bool StatsProfiling { get; set; }

        /// <summary> Horizontal alignment of the statistics overlay: 0 is the left screen edge, 1 the
        /// right one. </summary>
        [JsonProperty(Names.StatsAlignmentX)]
        [RuleInRange(0f, 1f)]
        public float StatsAlignmentX { get; set; }

        /// <summary> Vertical alignment of the statistics overlay: 0 is the bottom screen edge, 1 the
        /// top one. </summary>
        [JsonProperty(Names.StatsAlignmentY)]
        [RuleInRange(0f, 1f)]
        public float StatsAlignmentY { get; set; }

        /// <summary> What the main menu draws behind its buttons. </summary>
        [JsonProperty(Names.MenuBackground)]
        [RuleEnumValid]
        public MenuBackgroundKind MenuBackground { get; set; }

        // The SHELL's orientation: a running level's LevelSettings.Orientation outranks it (OrientationMath).
        // Phones only; on desktop the stored value is left untouched.

        /// <summary> Which way round the player asked the device to hold this game. </summary>
        [JsonProperty(Names.ScreenOrientation)]
        [RuleEnumValid]
        public ScreenOrientationLock ScreenOrientation { get; set; }

        // Hides what is DRAWN, never what is there (the pause button still takes a press): opacity, not
        // display or visibility. True by default, the behaviour the game already had.

        /// <summary> Whether the run progress bar is drawn. </summary>
        [JsonProperty(Names.ShowGameProgress)]
        public bool ShowGameProgress { get; set; }

        /// <summary> Whether the pause button is drawn. It stays pressable either way. </summary>
        [JsonProperty(Names.ShowGamePause)]
        public bool ShowGamePause { get; set; }

        /// <summary> The master switch over the other two, and over the on-screen touch controls
        /// with them: off, the game screen draws nothing at all. </summary>
        [JsonProperty(Names.ShowGameInterface)]
        public bool ShowGameInterface { get; set; }

        // A slider, ON by default: the drawn 1x1 body overstates the AvatarRules.CollisionScale hitbox.
        // 0 turns it off. Mirrors EditorSelectionSettings.ColliderOpacityView. Docs/SETTING_GROUPS.md

        /// <summary> How visible the avatar's hitbox ring is. 0 hides it entirely. </summary>
        [JsonProperty(Names.HitboxRingOpacity)]
        [RuleInRange(0f, 1f)]
        public float HitboxRingOpacity { get; set; }

        // True by default. Gates the WINDOW, never the report: ErrorReportService publishes every report.

        /// <summary> Whether an uncaught exception opens the error window. The report is built and
        /// logged either way. </summary>
        [JsonProperty(Names.AlertOnException)]
        public bool AlertOnException { get; set; }

        // Where the MENU's level browser starts; the editor's is EditorInterfaceSettings.LevelsLayout.

        /// <summary> How the menu's level browser lays its results out when it opens. </summary>
        [JsonProperty(Names.LevelsLayout)]
        [RuleEnumValid]
        public LevelBrowserLayout LevelsLayout { get; set; }

        /// <summary> What the hitbox ring's opacity is worth before a player touches it. </summary>
        public const float DefaultHitboxRingOpacity = 0.6f;

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public InterfaceSettings()
        {
            OpenMenuOnLose = false;
            StatsActive = false;
            StatsFrameObjects = false;
            StatsLevelObjects = false;
            StatsMemory = false;
            StatsProfiling = false;
            StatsAlignmentX = 0f;
            StatsAlignmentY = 1f;
            MenuBackground = MenuBackgroundKind.Bot;
            ScreenOrientation = ScreenOrientationLock.Unlock;
            ShowGameProgress = true;
            ShowGamePause = true;
            ShowGameInterface = true;
            HitboxRingOpacity = DefaultHitboxRingOpacity;
            AlertOnException = true;
            LevelsLayout = LevelBrowserLayout.Grid;
        }

        // The four true-by-default flags are not parameters (a source break for every caller); they are
        // defaulted here too, so a built value and a freshly defaulted one compare equal.

        /// <summary> Every member at once, in declaration order. </summary>
        public InterfaceSettings(bool openMenuOnLose, bool statsActive,
            float statsAlignmentX, float statsAlignmentY, MenuBackgroundKind menuBackground,
            ScreenOrientationLock screenOrientation)
        {
            OpenMenuOnLose = openMenuOnLose;
            StatsActive = statsActive;
            StatsAlignmentX = statsAlignmentX;
            StatsAlignmentY = statsAlignmentY;
            MenuBackground = menuBackground;
            ScreenOrientation = screenOrientation;
            ShowGameProgress = true;
            ShowGamePause = true;
            ShowGameInterface = true;
            HitboxRingOpacity = DefaultHitboxRingOpacity;
            AlertOnException = true;
            LevelsLayout = LevelBrowserLayout.Grid;
        }

        // Nested because HashCode.Combine takes eight arguments and there are nine values.
    }
}