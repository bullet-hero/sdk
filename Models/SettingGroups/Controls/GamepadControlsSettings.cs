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
    // Dash buttons live here rather than in a pad-wide block, because a gamepad is one device again: the
    // block existed to share them between the pad's sticks, its touchpad and its gyro, and the last two are
    // gone. Brand and glyph style went with it - both only meant anything next to a per-brand detector, and
    // detecting a brand meant reasoning about DualShock/XInput/Switch layouts rather than about a Gamepad.

    /// <summary>
    /// A gamepad, through the Input System's own layout: which stick steers, how its deflection is shaped,
    /// and which buttons dash.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class GamepadControlsSettings : BaseDeviceControlsSettings,
        IModel<GamepadControlsSettings>, IMoveable<GamepadControlsSettings>
    {
        [RuleEnumValid(GamepadControlMode.Direction)]
        [JsonProperty(Names.Mode)]
        public GamepadControlMode Mode { get; set; }

        [RuleEnumValid(MotionStick.Both)]
        [JsonProperty(Names.MotionStick)]
        public MotionStick MotionStick { get; set; }

        /// <summary> Exponent applied to stick deflection: 1 is linear, higher favours small
        /// movements. </summary>
        [RuleInRange(ControlsRules.MinResponseCurve, ControlsRules.MaxResponseCurve)]
        [JsonProperty(Names.ResponseCurve)]
        public float ResponseCurve { get; set; }

        /// <summary> Buttons that dash. Named by POSITION rather than by symbol, so a player switching pad
        /// families keeps the binding and only the glyph would change. </summary>
        [RuleEnumFlagsValid]
        [JsonProperty(Names.DashButtons)]
        public GamepadButtonMask DashButtons { get; set; }

        public override ControlMode GeneralMode => (ControlMode)Mode;
        public override ControlDevice Device => ControlDevice.Gamepad;

        public GamepadControlsSettings()
        {
            ResetOwn();
        }
        public GamepadControlsSettings(bool active, float sensitivity,
            float deadZone, float smoothing, bool invertX, bool invertY, GamepadControlMode mode,
            MotionStick motionStick, float responseCurve, GamepadButtonMask dashButtons)
            : base(active, sensitivity, deadZone, smoothing, invertX, invertY)
        {
            Mode = mode;
            MotionStick = motionStick;
            ResponseCurve = responseCurve;
            DashButtons = dashButtons;
        }
        private void ResetOwn()
        {
            Mode = GamepadControlMode.Direction;
            MotionStick = MotionStick.Both;
            ResponseCurve = ControlsRules.DefaultResponseCurve;

            // A pad's own sensitivity default, overriding the shared one from base.Reset() above: a stick
            // is a rate, not a position, so Relative mode moves the cursor by full deflection per second
            // and 1.0 of a camera per second reads as sluggish next to a mouse.
            Sensitivity = ControlsRules.DefaultGamepadSensitivity;
            DashButtons = GamepadButtonMask.South | GamepadButtonMask.RightShoulder;
        }
    }
}
