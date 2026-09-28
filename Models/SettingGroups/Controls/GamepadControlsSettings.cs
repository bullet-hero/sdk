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
    // There is no dash binding here any more: every button but Start and Select dashes, and Start pauses.
    // Choosing buttons was a setting nobody needed, and a pad handed to someone else then dashed on
    // whichever button they reached for first rather than on the two the owner had picked.

    /// <summary>
    /// A gamepad, through the Input System's own layout: which stick steers, and how its deflection is
    /// shaped.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class GamepadControlsSettings : BaseDeviceControlsSettings,
        IModel<GamepadControlsSettings>, IMoveable<GamepadControlsSettings>
    {
        /// <summary> Which of the three steering modes the pad drives. </summary>
        [RuleEnumValid(GamepadControlMode.Direction)]
        [JsonProperty(Names.Mode)]
        public GamepadControlMode Mode { get; set; }

        /// <summary> Which stick moves the avatar; the other one is free. </summary>
        [RuleEnumValid(MotionStick.Both)]
        [JsonProperty(Names.MotionStick)]
        public MotionStick MotionStick { get; set; }

        /// <summary> Exponent applied to stick deflection: 1 is linear, higher favours small
        /// movements. </summary>
        [RuleInRange(ControlsRules.MinResponseCurve, ControlsRules.MaxResponseCurve)]
        [JsonProperty(Names.ResponseCurve)]
        public float ResponseCurve { get; set; }

        /// <summary> This device's own mode as the device-independent one; the two enums line up by convention. </summary>
        public override ControlMode GeneralMode => (ControlMode)Mode;
        /// <summary> Which device these settings are for. </summary>
        public override ControlDevice Device => ControlDevice.Gamepad;

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public GamepadControlsSettings()
        {
            ResetOwn();
        }
        /// <summary> Every member at once, in declaration order. </summary>
        public GamepadControlsSettings(bool active, float sensitivity,
            float deadZone, float smoothing, bool invertX, bool invertY, GamepadControlMode mode,
            MotionStick motionStick, float responseCurve)
            : base(active, sensitivity, deadZone, smoothing, invertX, invertY)
        {
            Mode = mode;
            MotionStick = motionStick;
            ResponseCurve = responseCurve;
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
        }
    }
}
