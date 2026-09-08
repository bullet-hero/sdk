using System;
using BH.SDK.Models.Enums.Controls;
using BH.SDK.Models.Enums.Controls.Modes;

namespace BH.SDK.Services.Controls
{
    // The whole matrix is deliberately uniform - all four devices implement all three modes - and that is
    // why SupportedModes reads All everywhere today. The mask exists anyway, because it is the only place a
    // future device that genuinely cannot do one of them (a pedal, a wheel, a MIDI pad) can say so without
    // every consumer growing a special case.

    /// <summary>
    /// The static per-device facts, one entry per <see cref="ControlDevice"/>.
    /// </summary>
    public static class ControlDeviceCatalog
    {
        /// <summary> What a device's own localization key starts with. </summary>
        public const string NameKeyPrefix = "control_device_";

        /// <summary> Every device the format knows, whatever the running platform can reach. </summary>
        public static readonly ControlDevice[] Devices =
        {
            ControlDevice.KeyboardMouse,
            ControlDevice.Touchscreen,
            ControlDevice.Gamepad,
            ControlDevice.DeviceGyro,
        };

        /// <summary> How many there are. </summary>
        public static int DeviceCount => Devices.Length;

        private static readonly ControlDeviceInfo[] Infos =
        {
            new(ControlDevice.KeyboardMouse, ControlModeMask.All, true, NameKeyPrefix + "keyboard_mouse"),
            new(ControlDevice.Touchscreen, ControlModeMask.All, true, NameKeyPrefix + "touchscreen"),
            new(ControlDevice.Gamepad, ControlModeMask.All, true, NameKeyPrefix + "gamepad"),
            new(ControlDevice.DeviceGyro, ControlModeMask.All, true, NameKeyPrefix + "device_gyro"),
        };

        /// <summary> What one device supports. </summary>
        public static ControlDeviceInfo Get(ControlDevice device)
        {
            var index = (int)device;
            return index < Infos.Length ? Infos[index]
                : throw new ArgumentOutOfRangeException(nameof(device), device, "Unknown control device");
        }

        /// <summary> Which steering modes it can drive. </summary>
        public static ControlModeMask GetSupportedModes(ControlDevice device) => Get(device).SupportedModes;

        /// <summary> Whether it can drive one particular mode. </summary>
        public static bool Supports(ControlDevice device, ControlMode mode) => Get(device).Supports(mode);
    }
}
