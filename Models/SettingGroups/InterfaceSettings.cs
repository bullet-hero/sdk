using System;
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
    public class InterfaceSettings : IModel<InterfaceSettings>, IMoveable<InterfaceSettings>
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

        public InterfaceSettings()
        {
            OpenMenuOnLose = false;
            StatsActive = false;
            StatsAlignmentX = 0f;
            StatsAlignmentY = 1f;
            MenuBackground = MenuBackgroundKind.Bot;
        }

        public InterfaceSettings(bool openMenuOnLose, bool statsActive,
            float statsAlignmentX, float statsAlignmentY, MenuBackgroundKind menuBackground)
        {
            OpenMenuOnLose = openMenuOnLose;
            StatsActive = statsActive;
            StatsAlignmentX = statsAlignmentX;
            StatsAlignmentY = statsAlignmentY;
            MenuBackground = menuBackground;
        }

        public void Reset()
        {
            OpenMenuOnLose = false;
            StatsActive = false;
            StatsAlignmentX = 0f;
            StatsAlignmentY = 1f;
            MenuBackground = MenuBackgroundKind.Bot;
        }

        public object Clone() => Copy();

        public InterfaceSettings Copy() =>
            new(OpenMenuOnLose, StatsActive, StatsAlignmentX, StatsAlignmentY, MenuBackground);

        public void Pull(InterfaceSettings source)
        {
            OpenMenuOnLose = source.OpenMenuOnLose;
            StatsActive = source.StatsActive;
            StatsAlignmentX = source.StatsAlignmentX;
            StatsAlignmentY = source.StatsAlignmentY;
            MenuBackground = source.MenuBackground;
        }

        public void Update(InterfaceSettings src)
        {
            OpenMenuOnLose = src.OpenMenuOnLose;
            StatsActive = src.StatsActive;
            StatsAlignmentX = src.StatsAlignmentX;
            StatsAlignmentY = src.StatsAlignmentY;
            MenuBackground = src.MenuBackground;
        }

        public override int GetHashCode() =>
            HashCode.Combine(OpenMenuOnLose, StatsActive, StatsAlignmentX, StatsAlignmentY, MenuBackground);

        public override bool Equals(object obj) => obj is InterfaceSettings value && Equals(value);

        public bool Equals(InterfaceSettings other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return OpenMenuOnLose == other.OpenMenuOnLose
                   && StatsActive == other.StatsActive
                   && StatsAlignmentX.Equals(other.StatsAlignmentX)
                   && StatsAlignmentY.Equals(other.StatsAlignmentY)
                   && MenuBackground == other.MenuBackground;
        }
    }
}