using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Controls;
using BH.SDK.Models.Interfaces;
using Newtonsoft.Json;

namespace BH.SDK.Versions.V1
{
    // ReSharper disable once InconsistentNaming

    // Intentionally doesn't have ModelGeneration: a nested leaf of ControlsSettingsV1, frozen because
    // generation 2 took every form of locality out of the motion sensor - the calibration, the stored
    // neutral point, the axis mapping and the angular-velocity mode - and folded the tilt angle into
    // the shared Sensitivity. Flat rather than derived from BaseDeviceControlsSettings, so nothing the
    // live base does later can reach it.

    /// <summary> The motion-sensor group as generation 1 (Release) of the user-settings domain wrote
    /// it. A frozen snapshot - never edit it to match today's shape. </summary>
    [GenerateModel]
    public sealed partial class DeviceGyroControlsSettingsV1 : IModel<DeviceGyroControlsSettingsV1>
    {
        /// <summary> Whether the sensor may drive the avatar. </summary>
        [JsonProperty("active")]
        public bool Active { get; set; }

        /// <summary> Unread by the sensor at generation 1 - the tilt angle did its job. </summary>
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

        /// <summary> Which steering mode the sensor drove, <c>Relative</c> included. </summary>
        [JsonProperty("mode")]
        public DeviceGyroControlModeV1 Mode { get; set; }

        /// <summary> Which rotation axes became the screen axes. Gone at generation 2. </summary>
        [JsonProperty("axis_mapping")]
        public GyroAxisMappingV1 AxisMapping { get; set; }

        /// <summary> Re-zero on every level start. Gone at generation 2. </summary>
        [JsonProperty("calibrate_on_start")]
        public bool CalibrateOnStart { get; set; }

        /// <summary> Stored neutral point, X. Gone at generation 2. </summary>
        [JsonProperty("tilt_cntr_x")]
        public float TiltCenterX { get; set; }

        /// <summary> Stored neutral point, Y. Gone at generation 2. </summary>
        [JsonProperty("tilt_cntr_y")]
        public float TiltCenterY { get; set; }

        /// <summary> Degrees of tilt that read as full deflection. Folded into Sensitivity at
        /// generation 2. </summary>
        [JsonProperty("max_tilt_ang")]
        public float MaxTiltAngle { get; set; }

        /// <summary> How dash is triggered. </summary>
        [JsonProperty("dash_src")]
        public GyroDashSource DashSource { get; set; }

        /// <summary> Where the on-screen dash button sits. </summary>
        [JsonProperty("dash_button_anc")]
        public ScreenAnchor DashButtonAnchor { get; set; }

        /// <summary> How large that button is drawn. </summary>
        [JsonProperty("dash_button_sz")]
        public float DashButtonSize { get; set; }

        /// <summary> Every member at the value generation 1 would have read into it. </summary>
        public DeviceGyroControlsSettingsV1()
        {
            Active = true;
            Sensitivity = 1f;
            DeadZone = 0.05f;
            Smoothing = 0.06f;
            InvertX = false;
            InvertY = false;
            Mode = DeviceGyroControlModeV1.Direction;
            AxisMapping = GyroAxisMappingV1.RollPitch;
            CalibrateOnStart = true;
            TiltCenterX = 0f;
            TiltCenterY = 0f;
            MaxTiltAngle = 20f;
            DashSource = GyroDashSource.AnyScreenTap;
            DashButtonAnchor = ScreenAnchor.BottomRight;
            DashButtonSize = 0.18f;
        }
    }
}
