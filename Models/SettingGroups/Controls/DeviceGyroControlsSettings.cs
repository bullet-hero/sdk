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
    // TiltCenter is written by calibration rather than typed, and it is stored because it is a property
    // of how this player holds this device - recalibrating on every launch would move the neutral point
    // to wherever the phone happened to be lying. CalibrateOnStart is the opt-in for the opposite
    // preference.
    //
    // A phone's gyro has no buttons at all, hence DashSource: a tap anywhere is the default, since it
    // costs no screen space, and the on-screen button is for players who tap the play area by accident.

    /// <summary>
    /// The phone/tablet's own motion sensor: tilt as direction by default.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class DeviceGyroControlsSettings : BaseDeviceControlsSettings,
        IModel<DeviceGyroControlsSettings>, IMoveable<DeviceGyroControlsSettings>
    {
        [RuleEnumValid(DeviceGyroControlMode.Direction)]
        [JsonProperty(Names.Mode)]
        public DeviceGyroControlMode Mode { get; set; }

        [RuleEnumValid(GyroAxisMapping.RollPitch)]
        [JsonProperty(Names.AxisMapping)]
        public GyroAxisMapping AxisMapping { get; set; }

        /// <summary> Re-zero the neutral orientation on every level start, instead of keeping the
        /// stored <see cref="TiltCenterX"/>/<see cref="TiltCenterY"/>. </summary>
        [JsonProperty(Names.CalibrateOnStart)]
        public bool CalibrateOnStart { get; set; }

        /// <summary> The neutral orientation, in normalized deflection - written by calibration. </summary>
        [RuleInRange(ControlsRules.MinTiltCenter, ControlsRules.MaxTiltCenter)]
        [JsonProperty(Names.TiltCenterX)]
        public float TiltCenterX { get; set; }

        [RuleInRange(ControlsRules.MinTiltCenter, ControlsRules.MaxTiltCenter)]
        [JsonProperty(Names.TiltCenterY)]
        public float TiltCenterY { get; set; }

        /// <summary> Degrees of tilt that read as full deflection. </summary>
        [RuleInRange(ControlsRules.MinTiltAngle, ControlsRules.MaxTiltAngle)]
        [JsonProperty(Names.MaxTiltAngle)]
        public float MaxTiltAngle { get; set; }

        [RuleEnumValid(GyroDashSource.AnyScreenTap)]
        [JsonProperty(Names.DashSource)]
        public GyroDashSource DashSource { get; set; }

        [RuleEnumValid(ScreenAnchor.BottomRight)]
        [JsonProperty(Names.DashButtonAnchor)]
        public ScreenAnchor DashButtonAnchor { get; set; }

        [RuleInRange(ControlsRules.MinControlSize, ControlsRules.MaxControlSize)]
        [JsonProperty(Names.DashButtonSize)]
        public float DashButtonSize { get; set; }

        public override ControlMode GeneralMode => (ControlMode)Mode;
        public override ControlDevice Device => ControlDevice.DeviceGyro;

        public DeviceGyroControlsSettings()
        {
            ResetOwn();
        }
        public DeviceGyroControlsSettings(bool active, float sensitivity,
            float deadZone, float smoothing, bool invertX, bool invertY, DeviceGyroControlMode mode,
            GyroAxisMapping axisMapping, bool calibrateOnStart, float tiltCenterX, float tiltCenterY,
            float maxTiltAngle, GyroDashSource dashSource, ScreenAnchor dashButtonAnchor,
            float dashButtonSize)
            : base(active, sensitivity, deadZone, smoothing, invertX, invertY)
        {
            Mode = mode;
            AxisMapping = axisMapping;
            CalibrateOnStart = calibrateOnStart;
            TiltCenterX = tiltCenterX;
            TiltCenterY = tiltCenterY;
            MaxTiltAngle = maxTiltAngle;
            DashSource = dashSource;
            DashButtonAnchor = dashButtonAnchor;
            DashButtonSize = dashButtonSize;
        }
        private void ResetOwn()
        {
            // Both overwrite what BaseDeviceControlsSettings.Reset just wrote: a tilt is neither a stick
            // nor a mouse, and the two numbers it inherits are tuned for a switch a hand is not holding.
            DeadZone = ControlsRules.DefaultGyroDeadZone;
            Smoothing = ControlsRules.DefaultGyroSmoothing;

            Mode = DeviceGyroControlMode.Direction;
            AxisMapping = GyroAxisMapping.RollPitch;
            CalibrateOnStart = true;
            TiltCenterX = ControlsRules.DefaultTiltCenter;
            TiltCenterY = ControlsRules.DefaultTiltCenter;
            MaxTiltAngle = ControlsRules.DefaultTiltAngle;
            DashSource = GyroDashSource.AnyScreenTap;
            DashButtonAnchor = ScreenAnchor.BottomRight;
            DashButtonSize = ControlsRules.DefaultControlSize;
        }
    }
}
