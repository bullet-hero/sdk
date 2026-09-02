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
    // The Direction-only block at the bottom is the on-screen layout, and it is stored here rather than
    // in a UI settings group on purpose: it only exists because THIS device is in Direction mode, and a
    // player who never touches a touchscreen never sees it.
    //
    // Sizes are fractions of screen height and anchors are the nine reach positions, both so a layout
    // authored on one phone lands in the same place on another. Handedness mirrors the whole set at
    // once - mirroring elements one by one is how a left-handed layout ends up half right-handed.

    /// <summary>
    /// The device's own touchscreen: drag-to-move by default, with an on-screen stick in Direction
    /// mode.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class TouchscreenControlsSettings : BaseDeviceControlsSettings,
        IModel<TouchscreenControlsSettings>, IMoveable<TouchscreenControlsSettings>
    {
        [RuleEnumValid(TouchscreenControlMode.Relative)]
        [JsonProperty(Names.Mode)]
        public TouchscreenControlMode Mode { get; set; }

        /// <summary> Absolute mode: how far the cursor sits from the finger, as a fraction of camera
        /// height, so a thumb does not cover the avatar. </summary>
        [RuleInRange(ControlsRules.MinFingerOffset, ControlsRules.MaxFingerOffset)]
        [JsonProperty(Names.FingerOffsetX)]
        public float FingerOffsetX { get; set; }

        [RuleInRange(ControlsRules.MinFingerOffset, ControlsRules.MaxFingerOffset)]
        [JsonProperty(Names.FingerOffsetY)]
        public float FingerOffsetY { get; set; }

        /// <summary> A second finger anywhere on screen dashes - needs no on-screen button and never
        /// covers the play area. </summary>
        [JsonProperty(Names.DashOnSecondFinger)]
        public bool DashOnSecondFinger { get; set; }

        [JsonProperty(Names.DashOnDoubleTap)]
        public bool DashOnDoubleTap { get; set; }

        [RuleInRange(ControlsRules.MinDoubleTapTime, ControlsRules.MaxDoubleTapTime)]
        [JsonProperty(Names.DoubleTapTime)]
        public float DoubleTapTime { get; set; }

        /// <summary> How far a finger may travel and still count as a tap, as a fraction of screen
        /// height. </summary>
        [RuleInRange(ControlsRules.MinTapTravel, ControlsRules.MaxTapTravel)]
        [JsonProperty(Names.TapMaxTravel)]
        public float TapMaxTravel { get; set; }

        /// <summary> Mirrors the whole on-screen layout. </summary>
        [RuleEnumValid(Handedness.Right)]
        [JsonProperty(Names.Handedness)]
        public Handedness Handedness { get; set; }

        [RuleEnumValid(ScreenAnchor.BottomLeft)]
        [JsonProperty(Names.JoystickAnchor)]
        public ScreenAnchor JoystickAnchor { get; set; }

        [RuleInRange(ControlsRules.MinControlSize, ControlsRules.MaxControlSize)]
        [JsonProperty(Names.JoystickSize)]
        public float JoystickSize { get; set; }

        /// <summary> Pixels the knob travels before the stick reads as fully deflected. </summary>
        [RuleInRange(ControlsRules.MinJoystickTravel, ControlsRules.MaxJoystickTravel)]
        [JsonProperty(Names.JoystickTravel)]
        public float JoystickTravel { get; set; }

        /// <summary> The stick's origin follows the first touch instead of staying where it is
        /// drawn. </summary>
        [JsonProperty(Names.JoystickDynamicOrigin)]
        public bool JoystickDynamicOrigin { get; set; }

        [RuleEnumValid(ScreenAnchor.BottomRight)]
        [JsonProperty(Names.DashButtonAnchor)]
        public ScreenAnchor DashButtonAnchor { get; set; }

        [RuleInRange(ControlsRules.MinControlSize, ControlsRules.MaxControlSize)]
        [JsonProperty(Names.DashButtonSize)]
        public float DashButtonSize { get; set; }

        /// <summary> Which icon the dash button draws, by index into the game's own set. </summary>
        [RuleMinValue(0)]
        [JsonProperty(Names.DashButtonIcon)]
        public int DashButtonIcon { get; set; }

        public override ControlMode GeneralMode => (ControlMode)Mode;
        public override ControlDevice Device => ControlDevice.Touchscreen;

        public TouchscreenControlsSettings()
        {
            ResetOwn();
            DeadZone = ControlsRules.DefaultTouchDeadZone;
        }
        public TouchscreenControlsSettings(bool active, float sensitivity,
            float deadZone, float smoothing, bool invertX, bool invertY, TouchscreenControlMode mode,
            float fingerOffsetX, float fingerOffsetY, bool dashOnSecondFinger, bool dashOnDoubleTap,
            float doubleTapTime, float tapMaxTravel, Handedness handedness, ScreenAnchor joystickAnchor,
            float joystickSize, float joystickTravel, bool joystickDynamicOrigin,
            ScreenAnchor dashButtonAnchor, float dashButtonSize, int dashButtonIcon)
            : base(active, sensitivity, deadZone, smoothing, invertX, invertY)
        {
            Mode = mode;
            FingerOffsetX = fingerOffsetX;
            FingerOffsetY = fingerOffsetY;
            DashOnSecondFinger = dashOnSecondFinger;
            DashOnDoubleTap = dashOnDoubleTap;
            DoubleTapTime = doubleTapTime;
            TapMaxTravel = tapMaxTravel;
            Handedness = handedness;
            JoystickAnchor = joystickAnchor;
            JoystickSize = joystickSize;
            JoystickTravel = joystickTravel;
            JoystickDynamicOrigin = joystickDynamicOrigin;
            DashButtonAnchor = dashButtonAnchor;
            DashButtonSize = dashButtonSize;
            DashButtonIcon = dashButtonIcon;
        }
        private void ResetOwn()
        {
            Mode = TouchscreenControlMode.Relative;
            FingerOffsetX = ControlsRules.DefaultFingerOffsetX;
            FingerOffsetY = ControlsRules.DefaultFingerOffsetY;
            DashOnSecondFinger = true;
            DashOnDoubleTap = false;
            DoubleTapTime = ControlsRules.DefaultDoubleTapTime;
            TapMaxTravel = ControlsRules.DefaultTapTravel;
            Handedness = Handedness.Right;
            JoystickAnchor = ScreenAnchor.BottomLeft;
            JoystickSize = ControlsRules.DefaultControlSize;
            JoystickTravel = ControlsRules.DefaultJoystickTravel;
            JoystickDynamicOrigin = false;
            DashButtonAnchor = ScreenAnchor.BottomRight;
            DashButtonSize = ControlsRules.DefaultControlSize;
            DashButtonIcon = 0;
        }
    }
}
