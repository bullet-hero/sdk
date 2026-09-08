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
        /// <summary> Which of the three steering modes the sensor drives. </summary>
        [RuleEnumValid(DeviceGyroControlMode.Direction)]
        [JsonProperty(Names.Mode)]
        public DeviceGyroControlMode Mode { get; set; }

        /// <summary> Which two rotation axes become the two screen axes. </summary>
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

        /// <summary> The tilt treated as neutral, so a phone held at an angle still rests at the centre. </summary>
        [RuleInRange(ControlsRules.MinTiltCenter, ControlsRules.MaxTiltCenter)]
        [JsonProperty(Names.TiltCenterY)]
        public float TiltCenterY { get; set; }

        /// <summary> Degrees of tilt that read as full deflection. </summary>
        [RuleInRange(ControlsRules.MinTiltAngle, ControlsRules.MaxTiltAngle)]
        [JsonProperty(Names.MaxTiltAngle)]
        public float MaxTiltAngle { get; set; }

        /// <summary> How dash is triggered - the sensor itself has no buttons. </summary>
        [RuleEnumValid(GyroDashSource.AnyScreenTap)]
        [JsonProperty(Names.DashSource)]
        public GyroDashSource DashSource { get; set; }

        /// <summary> Where the on-screen dash button sits, when there is one. </summary>
        [RuleEnumValid(ScreenAnchor.BottomRight)]
        [JsonProperty(Names.DashButtonAnchor)]
        public ScreenAnchor DashButtonAnchor { get; set; }

        /// <summary> How large that button is drawn. </summary>
        [RuleInRange(ControlsRules.MinControlSize, ControlsRules.MaxControlSize)]
        [JsonProperty(Names.DashButtonSize)]
        public float DashButtonSize { get; set; }

        /// <summary> This device's own mode as the device-independent one; the two enums line up by convention. </summary>
        public override ControlMode GeneralMode => (ControlMode)Mode;
        /// <summary> Which device these settings are for. </summary>
        public override ControlDevice Device => ControlDevice.DeviceGyro;

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public DeviceGyroControlsSettings()
        {
            ResetOwn();
        }
        /// <summary> Every member at once, in declaration order. </summary>
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
