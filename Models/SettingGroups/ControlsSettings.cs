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
    /// How the player drives the game: the shared cursor settings and one group per input device.
    /// Which device leads is not stored - it is the most recently used one.
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
            KeyboardMouse = new KeyboardMouseControlsSettings();
            Touchscreen = new TouchscreenControlsSettings();
            Gamepad = new GamepadControlsSettings();
            DeviceGyro = new DeviceGyroControlsSettings();
        }
        /// <summary> Every member at once, in declaration order. </summary>
        public ControlsSettings(CommonControlsSettings common,
            KeyboardMouseControlsSettings keyboardMouse, TouchscreenControlsSettings touchscreen,
            GamepadControlsSettings gamepad, DeviceGyroControlsSettings deviceGyro)
        {
            Common = common;
            KeyboardMouse = keyboardMouse;
            Touchscreen = touchscreen;
            Gamepad = gamepad;
            DeviceGyro = deviceGyro;
        }

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
    }
}
