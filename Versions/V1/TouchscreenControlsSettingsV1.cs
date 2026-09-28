using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Controls;
using BH.SDK.Models.Enums.Controls.Modes;
using BH.SDK.Models.Interfaces;
using Newtonsoft.Json;

namespace BH.SDK.Versions.V1
{
    // ReSharper disable once InconsistentNaming

    // Intentionally doesn't have ModelGeneration: a nested leaf of ControlsSettingsV1, frozen because
    // generation 2 gave the live class cursor_return (a per-device answer to what lifting the finger
    // does). Flat rather than derived from BaseDeviceControlsSettings, so nothing the live base does
    // later can reach it.

    /// <summary> The touchscreen group as generation 1 (Release) of the user-settings domain wrote it -
    /// without <c>cursor_return</c>. A frozen snapshot - never edit it to match today's shape. </summary>
    [GenerateModel]
    public sealed partial class TouchscreenControlsSettingsV1 : IModel<TouchscreenControlsSettingsV1>
    {
        /// <summary> Whether the screen may drive the avatar. </summary>
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

        /// <summary> Which of the three steering modes the screen drives. </summary>
        [JsonProperty("mode")]
        public TouchscreenControlMode Mode { get; set; }

        /// <summary> Absolute mode: horizontal cursor offset from the finger. </summary>
        [JsonProperty("finger_off_x")]
        public float FingerOffsetX { get; set; }

        /// <summary> Absolute mode: vertical cursor offset from the finger. </summary>
        [JsonProperty("finger_off_y")]
        public float FingerOffsetY { get; set; }

        /// <summary> A second finger anywhere dashes. </summary>
        [JsonProperty("dash_on_second_finger")]
        public bool DashOnSecondFinger { get; set; }

        /// <summary> Dash by tapping twice. </summary>
        [JsonProperty("dash_on_double_tap")]
        public bool DashOnDoubleTap { get; set; }

        /// <summary> How close together those two taps have to be. </summary>
        [JsonProperty("double_tap_time")]
        public float DoubleTapTime { get; set; }

        /// <summary> How far a finger may travel and still count as a tap. </summary>
        [JsonProperty("tap_max_travel")]
        public float TapMaxTravel { get; set; }

        /// <summary> Mirrors the whole on-screen layout. </summary>
        [JsonProperty("handedness")]
        public Handedness Handedness { get; set; }

        /// <summary> Where the on-screen stick sits. </summary>
        [JsonProperty("joystick_anc")]
        public ScreenAnchor JoystickAnchor { get; set; }

        /// <summary> How large the stick is drawn. </summary>
        [JsonProperty("joystick_sz")]
        public float JoystickSize { get; set; }

        /// <summary> Pixels the knob travels before full deflection. </summary>
        [JsonProperty("joystick_travel")]
        public float JoystickTravel { get; set; }

        /// <summary> The stick's origin follows the first touch. </summary>
        [JsonProperty("joystick_dynamic_origin")]
        public bool JoystickDynamicOrigin { get; set; }

        /// <summary> Where the on-screen dash button sits. </summary>
        [JsonProperty("dash_button_anc")]
        public ScreenAnchor DashButtonAnchor { get; set; }

        /// <summary> How large that button is drawn. </summary>
        [JsonProperty("dash_button_sz")]
        public float DashButtonSize { get; set; }

        /// <summary> Which icon the dash button draws. </summary>
        [JsonProperty("dash_button_icon")]
        public int DashButtonIcon { get; set; }

        /// <summary> Every member at the value generation 1 would have read into it. </summary>
        public TouchscreenControlsSettingsV1()
        {
            Active = true;
            Sensitivity = 1f;
            DeadZone = 0.18f;
            Smoothing = 0f;
            InvertX = false;
            InvertY = false;
            Mode = TouchscreenControlMode.Relative;
            FingerOffsetX = 0f;
            FingerOffsetY = 0.15f;
            DashOnSecondFinger = true;
            DashOnDoubleTap = false;
            DoubleTapTime = 0.3f;
            TapMaxTravel = 0.05f;
            Handedness = Handedness.Right;
            JoystickAnchor = ScreenAnchor.BottomLeft;
            JoystickSize = 0.18f;
            JoystickTravel = 100f;
            JoystickDynamicOrigin = false;
            DashButtonAnchor = ScreenAnchor.BottomRight;
            DashButtonSize = 0.18f;
            DashButtonIcon = 0;
        }
    }
}
