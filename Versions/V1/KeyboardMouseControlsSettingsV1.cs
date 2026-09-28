using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Controls;
using BH.SDK.Models.Enums.Controls.Modes;
using BH.SDK.Models.Interfaces;
using Newtonsoft.Json;

namespace BH.SDK.Versions.V1
{
    // ReSharper disable once InconsistentNaming

    // Intentionally doesn't have ModelGeneration: a nested leaf of ControlsSettingsV1, frozen because
    // generation 2 gave the live class cursor_return (a per-device answer to what letting go does).
    // Flat rather than derived from BaseDeviceControlsSettings, so nothing the live base does later can
    // reach it.

    /// <summary> The keyboard and mouse group as generation 1 (Release) of the user-settings domain
    /// wrote it - without <c>cursor_return</c>. A frozen snapshot - never edit it to match today's
    /// shape. </summary>
    [GenerateModel]
    public sealed partial class KeyboardMouseControlsSettingsV1 : IModel<KeyboardMouseControlsSettingsV1>
    {
        /// <summary> Whether the pair may drive the avatar. </summary>
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

        /// <summary> Which of the three steering modes the pair drives. </summary>
        [JsonProperty("mode")]
        public KeyboardMouseControlMode Mode { get; set; }

        /// <summary> Steer only while the hold button is held. </summary>
        [JsonProperty("require_hold")]
        public bool RequireHold { get; set; }

        /// <summary> Which button has to be held. </summary>
        [JsonProperty("hold_button")]
        public MouseButton HoldButton { get; set; }

        /// <summary> Dash by clicking twice. </summary>
        [JsonProperty("dash_on_double_click")]
        public bool DashOnDoubleClick { get; set; }

        /// <summary> How close together those two clicks have to be. </summary>
        [JsonProperty("double_click_time")]
        public float DoubleClickTime { get; set; }

        /// <summary> Which keys request a dash. </summary>
        [JsonProperty("dash_keys")]
        public KeyBindingMask DashKeys { get; set; }

        /// <summary> Absolute mode: hide the OS cursor while steering. </summary>
        [JsonProperty("cursor_hide_abs")]
        public bool CursorHideAbsolute { get; set; }

        /// <summary> Relative mode: hide the OS cursor while steering. </summary>
        [JsonProperty("cursor_hide_rel")]
        public bool CursorHideRelative { get; set; }

        /// <summary> Which mouse button requests a dash. </summary>
        [JsonProperty("dash_button")]
        public MouseButton DashButton { get; set; }

        /// <summary> Every member at the value generation 1 would have read into it. </summary>
        public KeyboardMouseControlsSettingsV1()
        {
            Active = true;
            Sensitivity = 1f;
            DeadZone = 0.15f;
            Smoothing = 0f;
            InvertX = false;
            InvertY = false;
            Mode = KeyboardMouseControlMode.Absolute;
            RequireHold = true;
            HoldButton = MouseButton.Left;
            DashOnDoubleClick = true;
            DoubleClickTime = 0.3f;
            DashKeys = KeyBindingMask.Space | KeyBindingMask.Shift;
            CursorHideAbsolute = true;
            CursorHideRelative = false;
            DashButton = MouseButton.Right;
        }
    }
}
