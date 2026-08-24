using System;
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
    public class ControlsSettings : IModel<ControlsSettings>, IMoveable<ControlsSettings>
    {
        [RuleNotNull]
        [JsonProperty(Names.Common)]
        public CommonControlsSettings Common { get; set; }

        /// <summary> Every device exactly once: the order shown in the UI, the order a device is picked
        /// in at startup, and the tie-break when two are equally recently used. </summary>
        [RuleControlPriority]
        [JsonProperty(Names.Priority)]
        public ControlDevice[] Priority { get; set; }

        [RuleNotNull]
        [JsonProperty(Names.KeyboardMouse)]
        public KeyboardMouseControlsSettings KeyboardMouse { get; set; }

        [RuleNotNull]
        [JsonProperty(Names.Touchscreen)]
        public TouchscreenControlsSettings Touchscreen { get; set; }

        [RuleNotNull]
        [JsonProperty(Names.Gamepad)]
        public GamepadControlsSettings Gamepad { get; set; }

        [RuleNotNull]
        [JsonProperty(Names.DeviceGyro)]
        public DeviceGyroControlsSettings DeviceGyro { get; set; }

        public ControlsSettings()
        {
            Common = new CommonControlsSettings();
            Priority = DefaultPriority();
            KeyboardMouse = new KeyboardMouseControlsSettings();
            Touchscreen = new TouchscreenControlsSettings();
            Gamepad = new GamepadControlsSettings();
            DeviceGyro = new DeviceGyroControlsSettings();
        }
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
        public void Reset()
        {
            Common.Reset();
            Priority = DefaultPriority();
            KeyboardMouse.Reset();
            Touchscreen.Reset();
            Gamepad.Reset();
            DeviceGyro.Reset();
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

        public object Clone() => Copy();
        public ControlsSettings Copy() => new(Common.Copy(), (ControlDevice[])Priority.Clone(),
            (KeyboardMouseControlsSettings)KeyboardMouse.Copy(),
            (TouchscreenControlsSettings)Touchscreen.Copy(),
            (GamepadControlsSettings)Gamepad.Copy(),
            (DeviceGyroControlsSettings)DeviceGyro.Copy());

        public void Pull(ControlsSettings source)
        {
            Common.Pull(source.Common);
            Priority = (ControlDevice[])source.Priority.Clone();
            KeyboardMouse.Pull(source.KeyboardMouse);
            Touchscreen.Pull(source.Touchscreen);
            Gamepad.Pull(source.Gamepad);
            DeviceGyro.Pull(source.DeviceGyro);
        }

        public void Update(ControlsSettings src)
        {
            Common = src.Common.Copy();
            Priority = (ControlDevice[])src.Priority.Clone();
            KeyboardMouse = (KeyboardMouseControlsSettings)src.KeyboardMouse.Copy();
            Touchscreen = (TouchscreenControlsSettings)src.Touchscreen.Copy();
            Gamepad = (GamepadControlsSettings)src.Gamepad.Copy();
            DeviceGyro = (DeviceGyroControlsSettings)src.DeviceGyro.Copy();
        }

        public override bool Equals(object obj) => obj is ControlsSettings value && Equals(value);
        public override int GetHashCode()
        {
            var hash = HashCode.Combine(Common, KeyboardMouse, Touchscreen, Gamepad, DeviceGyro);

            if (Priority == null) return hash;
            foreach (var device in Priority)
                hash = HashCode.Combine(hash, device);
            return hash;
        }

        public bool Equals(ControlsSettings other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Common.Equals(other.Common)
                   && SamePriority(other.Priority)
                   && KeyboardMouse.Equals(other.KeyboardMouse)
                   && Touchscreen.Equals(other.Touchscreen)
                   && Gamepad.Equals(other.Gamepad)
                   && DeviceGyro.Equals(other.DeviceGyro);
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
