using System;
using BH.SDK.Models.Enums.Controls;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.Controls
{
    /// <summary>
    /// Settings above any single device: how the leading device is chosen, and how the shared in-world
    /// cursor looks.
    /// </summary>
    [RuleContainer]
    public class CommonControlsSettings : IModel<CommonControlsSettings>, IMoveable<CommonControlsSettings>
    {
        /// <summary> Auto lets the game follow the most recently used device; Manual pins one. </summary>
        [RuleEnumValid(DeviceSelection.Auto)]
        [JsonProperty(Names.Selection)]
        public DeviceSelection Selection { get; set; }

        /// <summary> The pinned device, read only while <see cref="Selection"/> is Manual. </summary>
        [RuleEnumValid(ControlDevice.KeyboardMouse)]
        [JsonProperty(Names.ManualDevice)]
        public ControlDevice ManualDevice { get; set; }

        /// <summary> Whether the in-world cursor is drawn at all. It still exists and still steers the
        /// avatar when hidden. </summary>
        [JsonProperty(Names.CursorVisible)]
        public bool CursorVisible { get; set; }

        [RuleInRange(ControlsRules.MinCursorScale, ControlsRules.MaxCursorScale)]
        [JsonProperty(Names.CursorScale)]
        public float CursorScale { get; set; }

        /// <summary> The cursor is born on the avatar rather than where it was left. </summary>
        [JsonProperty(Names.CursorRecenter)]
        public bool CursorRecenter { get; set; }

        // Off by default, because the two behaviours are opposite kinds of control and neither is
        // wrong: left where it was, the cursor is a place the avatar walks to and keeps walking to
        // after the button is up; returned to the avatar, letting go is a hard stop. Steering by
        // pointing wants the first, steering by dragging wants the second.

        /// <summary> The cursor snaps back onto the avatar the moment steering stops. </summary>
        [JsonProperty(Names.CursorReturn)]
        public bool CursorReturn { get; set; }

        public CommonControlsSettings()
        {
            Reset();
        }
        public CommonControlsSettings(DeviceSelection selection, ControlDevice manualDevice,
            bool cursorVisible, float cursorScale, bool cursorRecenter, bool cursorReturn)
        {
            Selection = selection;
            ManualDevice = manualDevice;
            CursorVisible = cursorVisible;
            CursorScale = cursorScale;
            CursorRecenter = cursorRecenter;
            CursorReturn = cursorReturn;
        }
        public void Reset()
        {
            Selection = DeviceSelection.Auto;
            ManualDevice = ControlDevice.KeyboardMouse;
            CursorVisible = true;
            CursorScale = ControlsRules.DefaultCursorScale;
            CursorRecenter = true;
            CursorReturn = false;
        }

        public object Clone() => Copy();
        public CommonControlsSettings Copy() => new(Selection, ManualDevice,
            CursorVisible, CursorScale, CursorRecenter, CursorReturn);

        public void Pull(CommonControlsSettings source)
        {
            Selection = source.Selection;
            ManualDevice = source.ManualDevice;
            CursorVisible = source.CursorVisible;
            CursorScale = source.CursorScale;
            CursorRecenter = source.CursorRecenter;
            CursorReturn = source.CursorReturn;
        }

        public void Update(CommonControlsSettings src)
        {
            Selection = src.Selection;
            ManualDevice = src.ManualDevice;
            CursorVisible = src.CursorVisible;
            CursorScale = src.CursorScale;
            CursorRecenter = src.CursorRecenter;
            CursorReturn = src.CursorReturn;
        }

        public override bool Equals(object obj) => obj is CommonControlsSettings value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(Selection, ManualDevice,
            CursorVisible, CursorScale, CursorRecenter, CursorReturn);

        public bool Equals(CommonControlsSettings other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Selection == other.Selection
                   && ManualDevice == other.ManualDevice
                   && CursorVisible == other.CursorVisible
                   && CursorScale.Equals(other.CursorScale)
                   && CursorRecenter == other.CursorRecenter
                   && CursorReturn == other.CursorReturn;
        }
    }
}
