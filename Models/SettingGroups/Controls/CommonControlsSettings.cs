using System;
using BH.SDK.Models.Attributes;
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
    [GenerateModel]
    public sealed partial class CommonControlsSettings : IModel<CommonControlsSettings>, IMoveable<CommonControlsSettings>
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
            Selection = DeviceSelection.Auto;
            ManualDevice = ControlDevice.KeyboardMouse;
            CursorVisible = true;
            CursorScale = ControlsRules.DefaultCursorScale;
            CursorRecenter = true;
            CursorReturn = false;
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
    }
}
