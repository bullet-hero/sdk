using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups
{
    // The alignment pair is stored as two free floats rather than as a nine-value enum, because it is
    // the same alignment convention level content itself is authored in (0,0 is the lower-left corner,
    // 1,1 the upper-right one) - the settings screen only offers the nine corner/edge/centre presets,
    // but a hand-edited settings.json placing the readout at 0.25,0.9 is legal data, not a value to
    // repair. Anything outside [0,1] would park the overlay off screen, which is what the range rule
    // is for.

    /// <summary>
    /// Device-wide options for the game's own interface overlays - today the diagnostics readout that
    /// every screen can draw over itself. Nothing here travels with a level.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class InterfaceSettings : IModel<InterfaceSettings>, IMoveable<InterfaceSettings>
    {
        // FALSE BY DEFAULT, and that is the whole reason this exists: a death used to be an
        // unconditional stop into the result window, which is four clicks of chrome between a player
        // and the retry they already decided on. Off, a lost run rewinds itself to the last
        // checkpoint (Services.Game's CheckpointService); on, the window is what a player who wants
        // to read the outcome, change a setting or leave gets.

        /// <summary> Whether losing a run opens the result window instead of respawning. </summary>
        [JsonProperty(Names.OpenMenuOnLose)]
        public bool OpenMenuOnLose { get; set; }

        /// <summary> Whether the statistics overlay is drawn at all. </summary>
        [JsonProperty(Names.StatsActive)]
        public bool StatsActive { get; set; }

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

        // THE SHELL'S ORIENTATION, NOT THE GAME'S. A running level's own LevelSettings.Orientation
        // outranks this for as long as it is running, so a player who finds their setting apparently
        // ignored inside a level is seeing the level win, which is by design. Core's OrientationMath
        // is the only place that ladder is written down.
        //
        // Honoured on phones only. On desktop nothing rotates, so the setting is not applied and the
        // settings screen shows the window's MEASURED orientation there instead - the stored value
        // below is left untouched rather than overwritten, so a value chosen on a phone survives the
        // same account opening the game on a PC.

        /// <summary> Which way round the player asked the device to hold this game. </summary>
        [JsonProperty(Names.ScreenOrientation)]
        [RuleEnumValid]
        public ScreenOrientationLock ScreenOrientation { get; set; }

        // WHAT IS DRAWN, NEVER WHAT IS THERE. All three hide the HUD visually and leave it exactly
        // as functional: the pause button still takes a press where it always sat, so a player who
        // hid it for a clean screen or for a recording has not also lost the way out of a run. That
        // is the whole contract, and it decides the implementation on the other side - opacity, not
        // display or visibility, since both of those stop an element being picked at all.
        //
        // TRUE BY DEFAULT, all three, so a settings file written before them reads back as the HUD
        // the game already had. Like MenuBackgroundKind.Bot and ScreenOrientationLock.Horizontal,
        // the default is the behaviour rather than the zero value - which is what makes them
        // additive with no DataVersion bump and no migrator.

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

        public InterfaceSettings()
        {
            OpenMenuOnLose = false;
            StatsActive = false;
            StatsAlignmentX = 0f;
            StatsAlignmentY = 1f;
            MenuBackground = MenuBackgroundKind.Bot;
            ScreenOrientation = ScreenOrientationLock.Horizontal;
            ShowGameProgress = true;
            ShowGamePause = true;
            ShowGameInterface = true;
        }

        // THE THREE HUD FLAGS ARE NOT PARAMETERS, and that is deliberate: adding one here is a
        // source break for every caller, and LevelSettings.Seed already set the precedent of
        // taking the object-initializer route in Copy instead. They are defaulted here as well as
        // in the parameterless constructor, so the two agree - a value built through this one and
        // a freshly defaulted one have to compare equal.
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
        }

        // Nested because HashCode.Combine takes eight arguments and there are nine values.
    }
}