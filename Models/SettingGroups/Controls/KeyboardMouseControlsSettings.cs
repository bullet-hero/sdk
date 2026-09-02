using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Controls;
using BH.SDK.Models.Enums.Controls.Modes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.Controls
{
    /// <summary>
    /// Keyboard and mouse: the PC default, following the mouse cursor while a button is held.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class KeyboardMouseControlsSettings : BaseDeviceControlsSettings,
        IModel<KeyboardMouseControlsSettings>, IMoveable<KeyboardMouseControlsSettings>
    {
        [RuleEnumValid(KeyboardMouseControlMode.Absolute)]
        [JsonProperty(Names.Mode)]
        public KeyboardMouseControlMode Mode { get; set; }

        /// <summary> Both cursor modes: steer only while <see cref="HoldButton"/> is held. Off makes the
        /// avatar chase the mouse permanently, across the HUD and every menu with it. </summary>
        [JsonProperty(Names.RequireHold)]
        public bool RequireHold { get; set; }

        [RuleEnumValid(MouseButton.Left)]
        [JsonProperty(Names.HoldButton)]
        public MouseButton HoldButton { get; set; }

        [JsonProperty(Names.DashOnDoubleClick)]
        public bool DashOnDoubleClick { get; set; }

        [RuleInRange(ControlsRules.MinDoubleClickTime, ControlsRules.MaxDoubleClickTime)]
        [JsonProperty(Names.DoubleClickTime)]
        public float DoubleClickTime { get; set; }

        // Space and Shift both, because Space is the dash key players arrive with. The level editor also
        // binds Space to play/pause, and that conflict is resolved where it exists rather than by taking
        // the key away from everyone: the editor CLAIMS Space (ControlService.ClaimKeys), so the driver
        // never reads it on that one screen and the setting stays what the player set.

        /// <summary> Which keys request a dash. </summary>
        [RuleEnumFlagsValid]
        [JsonProperty(Names.DashKeys)]
        public KeyBindingMask DashKeys { get; set; }

        // Hidden only WHILE steering, and never captured: the arrow comes back the moment the button is
        // up, so it is always there for the pause button and every menu. The two modes default apart
        // because they answer different questions - Absolute puts the avatar where the arrow is, so the
        // arrow is redundant with the in-world cursor drawn on top of it, while Relative moves the
        // in-world cursor BY the arrow, and losing sight of the arrow costs the player the thing they
        // are pushing with.

        /// <summary> Absolute mode: hide the OS cursor while the hold button is down. </summary>
        [JsonProperty(Names.HideCursorAbsolute)]
        public bool HideCursorAbsolute { get; set; }

        /// <summary> Relative mode: hide the OS cursor while the hold button is down. </summary>
        [JsonProperty(Names.HideCursorRelative)]
        public bool HideCursorRelative { get; set; }

        public override ControlMode GeneralMode => (ControlMode)Mode;
        public override ControlDevice Device => ControlDevice.KeyboardMouse;

        public KeyboardMouseControlsSettings()
        {
            ResetOwn();
        }
        public KeyboardMouseControlsSettings(bool active, float sensitivity,
            float deadZone, float smoothing, bool invertX, bool invertY, KeyboardMouseControlMode mode,
            bool requireHold, MouseButton holdButton, bool dashOnDoubleClick,
            float doubleClickTime, KeyBindingMask dashKeys, bool hideCursorAbsolute,
            bool hideCursorRelative)
            : base(active, sensitivity, deadZone, smoothing, invertX, invertY)
        {
            Mode = mode;
            RequireHold = requireHold;
            HoldButton = holdButton;
            DashOnDoubleClick = dashOnDoubleClick;
            DoubleClickTime = doubleClickTime;
            DashKeys = dashKeys;
            HideCursorAbsolute = hideCursorAbsolute;
            HideCursorRelative = hideCursorRelative;
        }
        private void ResetOwn()
        {
            Mode = KeyboardMouseControlMode.Absolute;
            RequireHold = true;
            HoldButton = MouseButton.Left;
            DashOnDoubleClick = true;
            DoubleClickTime = ControlsRules.DefaultDoubleClickTime;
            DashKeys = KeyBindingMask.Space | KeyBindingMask.Shift;
            HideCursorAbsolute = true;
            HideCursorRelative = false;
        }

        // The last slot is a nested Combine: HashCode.Combine tops out at eight arguments.
    }
}
