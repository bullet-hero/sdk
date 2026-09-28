using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Controls;
using BH.SDK.Models.Enums.Controls.Modes;
using BH.SDK.Models.Interfaces;
using Newtonsoft.Json;

namespace BH.SDK.Versions.V1
{
    // ReSharper disable once InconsistentNaming

    // Intentionally doesn't have ModelGeneration: a nested leaf of ControlsSettingsV1, frozen because
    // generation 2 removed DashButtons (every button but Start/Select dashes now). Flat rather than
    // derived from BaseDeviceControlsSettings, so nothing the live base does later can reach it.

    /// <summary> The gamepad group as generation 1 (Release) of the user-settings domain wrote it -
    /// with <c>dash_buttons</c>. A frozen snapshot - never edit it to match today's shape. </summary>
    [GenerateModel]
    public sealed partial class GamepadControlsSettingsV1 : IModel<GamepadControlsSettingsV1>
    {
        /// <summary> Whether the pad may drive the avatar. </summary>
        [JsonProperty("active")]
        public bool Active { get; set; }

        /// <summary> Multiplier on cursor deltas in Relative mode. </summary>
        [JsonProperty("sens")]
        public float Sensitivity { get; set; }

        /// <summary> Deflection below which input reads as nothing. </summary>
        [JsonProperty("dead_zone")]
        public float DeadZone { get; set; }

        /// <summary> How much of the previous frame's input is carried over. </summary>
        [JsonProperty("smoothing")]
        public float Smoothing { get; set; }

        /// <summary> Flip the horizontal axis. </summary>
        [JsonProperty("invert_x")]
        public bool InvertX { get; set; }

        /// <summary> Flip the vertical axis. </summary>
        [JsonProperty("invert_y")]
        public bool InvertY { get; set; }

        /// <summary> Which steering mode the pad drives. </summary>
        [JsonProperty("mode")]
        public GamepadControlMode Mode { get; set; }

        /// <summary> Which stick moves the avatar. </summary>
        [JsonProperty("motion_stick")]
        public MotionStick MotionStick { get; set; }

        /// <summary> Exponent applied to stick deflection. </summary>
        [JsonProperty("response_crv")]
        public float ResponseCurve { get; set; }

        /// <summary> Buttons that dashed. Gone at generation 2. </summary>
        [JsonProperty("dash_buttons")]
        public GamepadButtonMask DashButtons { get; set; }

        /// <summary> Every member at the value generation 1 would have read into it. </summary>
        public GamepadControlsSettingsV1()
        {
            Active = true;
            Sensitivity = 2f;
            DeadZone = 0.15f;
            Smoothing = 0f;
            InvertX = false;
            InvertY = false;
            Mode = GamepadControlMode.Direction;
            MotionStick = MotionStick.Both;
            ResponseCurve = 1f;
            DashButtons = GamepadButtonMask.South | GamepadButtonMask.RightShoulder;
        }
    }
}
