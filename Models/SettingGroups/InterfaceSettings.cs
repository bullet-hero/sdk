using System;
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

        public InterfaceSettings()
        {
            StatsActive = false;
            StatsAlignmentX = 0f;
            StatsAlignmentY = 1f;
        }
        public InterfaceSettings(bool statsActive, float statsAlignmentX, float statsAlignmentY)
        {
            StatsActive = statsActive;
            StatsAlignmentX = statsAlignmentX;
            StatsAlignmentY = statsAlignmentY;
        }
        public void Reset()
        {
            StatsActive = false;
            StatsAlignmentX = 0f;
            StatsAlignmentY = 1f;
        }

        public object Clone() => Copy();
        public InterfaceSettings Copy() => new(StatsActive, StatsAlignmentX, StatsAlignmentY);

        public void Pull(InterfaceSettings source)
        {
            StatsActive = source.StatsActive;
            StatsAlignmentX = source.StatsAlignmentX;
            StatsAlignmentY = source.StatsAlignmentY;
        }

        public override int GetHashCode() => HashCode.Combine(StatsActive, StatsAlignmentX, StatsAlignmentY);
        public override bool Equals(object obj) => obj is InterfaceSettings value && Equals(value);

        public bool Equals(InterfaceSettings other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return StatsActive == other.StatsActive
                   && StatsAlignmentX.Equals(other.StatsAlignmentX)
                   && StatsAlignmentY.Equals(other.StatsAlignmentY);
        }
    }
}
