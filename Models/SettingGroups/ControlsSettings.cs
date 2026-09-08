using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Controls;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.SettingGroups.Controls;
using BH.SDK.Rules.Attributes;
using BH.SDK.Services.Controls;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups
{
    /// <summary>
    /// How the player drives the game: the shared cursor/selection settings, the device priority
    /// order, and one group per input device.
    /// </summary>
    [RuleContainer]
    [RuleAnyDeviceActive]
    [GenerateModel]
    public sealed partial class ControlsSettings : IModel<ControlsSettings>, IMoveable<ControlsSettings>
    {
        /// <summary> What applies whichever device is steering. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Common)]
        public CommonControlsSettings Common { get; set; }

        /// <summary> Every device exactly once: the order shown in the UI, the order a device is picked
        /// in at startup, and the tie-break when two are equally recently used. </summary>
        [RuleControlPriority]
        [JsonProperty(Names.Priority)]
        public ControlDevice[] Priority { get; set; }

        /// <summary> Keyboard and mouse, which are one device here - they are never used apart. </summary>
        [RuleNotNull]
        [JsonProperty(Names.KeyboardMouse)]
        public KeyboardMouseControlsSettings KeyboardMouse { get; set; }

        /// <summary> The device's own touchscreen. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Touchscreen)]
        public TouchscreenControlsSettings Touchscreen { get; set; }

        /// <summary> A gamepad, through the Input System's own layout. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Gamepad)]
        public GamepadControlsSettings Gamepad { get; set; }

        /// <summary> The phone or tablet's own motion sensor. </summary>
        [RuleNotNull]
        [JsonProperty(Names.DeviceGyro)]
        public DeviceGyroControlsSettings DeviceGyro { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public ControlsSettings()
        {
            Common = new CommonControlsSettings();
            Priority = DefaultPriority();
            KeyboardMouse = new KeyboardMouseControlsSettings();
            Touchscreen = new TouchscreenControlsSettings();
            Gamepad = new GamepadControlsSettings();
            DeviceGyro = new DeviceGyroControlsSettings();
        }
        /// <summary> Every member at once, in declaration order. </summary>
        public ControlsSettings(CommonControlsSettings common, ControlDevice[] priority,
            KeyboardMouseControlsSettings keyboardMouse, TouchscreenControlsSettings touchscreen,
            GamepadControlsSettings gamepad, DeviceGyroControlsSettings deviceGyro)
        {
            Common = common;
            Priority = priority;
            KeyboardMouse = keyboardMouse;
            Touchscreen = touchscreen;
            Gamepad = gamepad;
            DeviceGyro = deviceGyro;
        }

        /// <summary> The catalog's own order. Which order actually SHIPS per platform is the consumer's
        /// call - the format only guarantees a valid permutation. </summary>
        public static ControlDevice[] DefaultPriority() => (ControlDevice[])ControlDeviceCatalog.Devices.Clone();

        /// <summary> One device's group, so a consumer can walk all six without a switch per call
        /// site. </summary>
        public BaseDeviceControlsSettings GetDevice(ControlDevice device) => device switch
        {
            ControlDevice.KeyboardMouse => KeyboardMouse,
            ControlDevice.Touchscreen => Touchscreen,
            ControlDevice.Gamepad => Gamepad,
            ControlDevice.DeviceGyro => DeviceGyro,
            _ => throw new ArgumentOutOfRangeException(nameof(device), device, "Unknown control device"),
        };

        /// <summary> Whether any device is active at all - the invariant a player must never be able to
        /// break, since clearing the last one leaves the game uncontrollable. </summary>
        public bool HasActiveDevice()
        {
            foreach (var device in ControlDeviceCatalog.Devices)
                if (GetDevice(device).Active) return true;
            return false;
        }

        private bool SamePriority(ControlDevice[] other)
        {
            if (ReferenceEquals(Priority, other)) return true;
            if (Priority == null || other == null) return false;
            if (Priority.Length != other.Length) return false;

            for (var i = 0; i < Priority.Length; i++)
                if (Priority[i] != other[i]) return false;
            return true;
        }
    }
}
