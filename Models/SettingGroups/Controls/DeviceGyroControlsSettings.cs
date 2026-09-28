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
    // NOTHING HERE IS LOCAL. Rest is the phone lying flat, screen up, read against gravity - there is no
    // calibration, no stored neutral point and no axis mapping, because every one of them made the
    // neutral pose depend on how the phone happened to be held at some earlier moment, and players read
    // that as the sensor being broken. How far a tilt reaches is the shared Sensitivity: 1 is full
    // deflection at ControlsRules.DefaultTiltAngle. Relative (angular velocity) is gone with them - it
    // has no absolute up at all and drifts by construction.
    //
    // A phone's gyro has no buttons at all, hence DashSource: a tap anywhere is the default, since it
    // costs no screen space, and the on-screen button is for players who tap the play area by accident.

    /// <summary>
    /// The phone/tablet's own motion sensor: tilt away from lying flat, as direction by default.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class DeviceGyroControlsSettings : BaseDeviceControlsSettings,
        IModel<DeviceGyroControlsSettings>, IMoveable<DeviceGyroControlsSettings>
    {
        /// <summary> Which steering mode the sensor drives - Absolute or Direction. </summary>
        [RuleEnumValid(DeviceGyroControlMode.Direction)]
        [JsonProperty(Names.Mode)]
        public DeviceGyroControlMode Mode { get; set; }

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
            GyroDashSource dashSource, ScreenAnchor dashButtonAnchor, float dashButtonSize)
            : base(active, sensitivity, deadZone, smoothing, invertX, invertY)
        {
            Mode = mode;
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
            DashSource = GyroDashSource.AnyScreenTap;
            DashButtonAnchor = ScreenAnchor.BottomRight;
            DashButtonSize = ControlsRules.DefaultControlSize;
        }
    }
}
