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
        /// <summary> Which of the three steering modes the pair drives. </summary>
        [RuleEnumValid(KeyboardMouseControlMode.Absolute)]
        [JsonProperty(Names.Mode)]
        public KeyboardMouseControlMode Mode { get; set; }

        /// <summary> Both cursor modes: steer only while <see cref="HoldButton"/> is held. Off makes the
        /// avatar chase the mouse permanently, across the HUD and every menu with it. </summary>
        [JsonProperty(Names.RequireHold)]
        public bool RequireHold { get; set; }

        /// <summary> Which button has to be held for the relative modes to track the mouse. </summary>
        [RuleEnumValid(MouseButton.Left)]
        [JsonProperty(Names.HoldButton)]
        public MouseButton HoldButton { get; set; }

        /// <summary> Trigger a dash by clicking twice, rather than only by its own key. </summary>
        [JsonProperty(Names.DashOnDoubleClick)]
        public bool DashOnDoubleClick { get; set; }

        /// <summary> How close together those two clicks have to be. </summary>
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
        [JsonProperty(Names.CursorHideAbsolute)]
        public bool CursorHideAbsolute { get; set; }

        /// <summary> Relative mode: hide the OS cursor while the hold button is down. </summary>
        [JsonProperty(Names.CursorHideRelative)]
        public bool CursorHideRelative { get; set; }

        // Declared LAST because the blob writes members in declaration order and that order is
        // append-only (see the SDK's VERSIONING record), not because it belongs at the bottom.
        //
        // The point of it is playing with ONE hand: the left button already steers, so binding the
        // dash to the right one makes the mouse a complete controller and the keyboard optional. It
        // is deliberately NOT excluded when it equals HoldButton - a press is an edge and a hold is a
        // level, so the same button can legitimately do both, exactly as DashOnDoubleClick already
        // does with the hold button itself.

        /// <summary> Which mouse button requests a dash; <see cref="MouseButton.None"/> unbinds it. </summary>
        [RuleEnumValid(MouseButton.Right)]
        [JsonProperty(Names.DashButton)]
        public MouseButton DashButton { get; set; }

        /// <summary> This device's own mode as the device-independent one; the two enums line up by convention. </summary>
        public override ControlMode GeneralMode => (ControlMode)Mode;
        /// <summary> Which device these settings are for. </summary>
        public override ControlDevice Device => ControlDevice.KeyboardMouse;

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public KeyboardMouseControlsSettings()
        {
            ResetOwn();
        }
        /// <summary> Every member at once, in declaration order. </summary>
        public KeyboardMouseControlsSettings(bool active, float sensitivity,
            float deadZone, float smoothing, bool invertX, bool invertY, KeyboardMouseControlMode mode,
            bool requireHold, MouseButton holdButton, bool dashOnDoubleClick,
            float doubleClickTime, KeyBindingMask dashKeys, bool hideCursorAbsolute,
            bool hideCursorRelative, MouseButton dashButton)
            : base(active, sensitivity, deadZone, smoothing, invertX, invertY)
        {
            Mode = mode;
            RequireHold = requireHold;
            HoldButton = holdButton;
            DashOnDoubleClick = dashOnDoubleClick;
            DoubleClickTime = doubleClickTime;
            DashKeys = dashKeys;
            CursorHideAbsolute = hideCursorAbsolute;
            CursorHideRelative = hideCursorRelative;
            DashButton = dashButton;
        }
        private void ResetOwn()
        {
            Mode = KeyboardMouseControlMode.Absolute;
            RequireHold = true;
            HoldButton = MouseButton.Left;
            DashOnDoubleClick = true;
            DoubleClickTime = ControlsRules.DefaultDoubleClickTime;
            DashKeys = KeyBindingMask.Space | KeyBindingMask.Shift;
            CursorHideAbsolute = true;
            CursorHideRelative = false;
            DashButton = MouseButton.Right;
        }

        // The last slot is a nested Combine: HashCode.Combine tops out at eight arguments.
    }
}
