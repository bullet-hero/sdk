using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Controls;
using BH.SDK.Models.Enums.Controls.Modes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Services.Controls;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups.Controls
{
    // Everything every control device shares, and deliberately NOT the mode: each device stores its
    // own mode enum instead, so an unsupported mode is unrepresentable rather than merely invalid, and
    // dropping a mode from one device touches nothing else. GeneralMode is the read side of that - the
    // one place a device's own enum is mapped onto the shared ControlMode vocabulary.
    //
    // These settings describe how the AVATAR is steered, and nothing else. An earlier design let each
    // device declare which scenes it may drive (game / menu / editor); that abstraction is gone, because
    // the menu and the level editor drive themselves - their own pointer, focus and shortcut handling -
    // and never asked this tree anything.

    /// <summary>
    /// Base of every per-device control group: whether the device is in play, and the four tuning knobs
    /// every device has.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public abstract partial class BaseDeviceControlsSettings : IModel<BaseDeviceControlsSettings>
    {
        /// <summary> Whether this device may drive the avatar at all. At least one device among the
        /// platform's permitted ones must stay active - see ControlsSettings. </summary>
        [JsonProperty(Names.Active)]
        public bool Active { get; set; }

        /// <summary> Multiplier on cursor deltas in Relative mode. </summary>
        [RuleInRange(ControlsRules.MinSensitivity, ControlsRules.MaxSensitivity)]
        [JsonProperty(Names.Sensitivity)]
        public float Sensitivity { get; set; }

        /// <summary> Deflection below which input reads as nothing - Absolute and Direction. </summary>
        [RuleInRange(ControlsRules.MinDeadZone, ControlsRules.MaxDeadZone)]
        [JsonProperty(Names.DeadZone)]
        public float DeadZone { get; set; }

        /// <summary> How much of the previous frame's input is carried over. 0 disables. </summary>
        [RuleInRange(ControlsRules.MinSmoothing, ControlsRules.MaxSmoothing)]
        [JsonProperty(Names.Smoothing)]
        public float Smoothing { get; set; }

        [JsonProperty(Names.InvertX)]
        public bool InvertX { get; set; }

        [JsonProperty(Names.InvertY)]
        public bool InvertY { get; set; }

        /// <summary> This device's own mode, expressed in the shared vocabulary. </summary>
        [JsonIgnore]
        public abstract ControlMode GeneralMode { get; }

        /// <summary> Which device this group belongs to - what lets ControlsSettings enumerate its six
        /// groups generically without a switch per call site. </summary>
        [JsonIgnore]
        public abstract ControlDevice Device { get; }

        protected BaseDeviceControlsSettings()
        {
            Active = true;
            Sensitivity = ControlsRules.DefaultSensitivity;
            DeadZone = ControlsRules.DefaultDeadZone;
            Smoothing = ControlsRules.DefaultSmoothing;
            InvertX = false;
            InvertY = false;
        }
        protected BaseDeviceControlsSettings(bool active, float sensitivity,
            float deadZone, float smoothing, bool invertX, bool invertY)
        {
            Active = active;
            Sensitivity = sensitivity;
            DeadZone = deadZone;
            Smoothing = smoothing;
            InvertX = invertX;
            InvertY = invertY;
        }
    }
}
