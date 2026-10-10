using BH.SDK.Models.Enums.Controls;
using BH.SDK.Models.Enums.Controls.Modes;

namespace BH.SDK.Services.Controls
{
    // Static facts about a device; not in the settings tree because they must not survive a file (a saved
    // "supports Relative" would outlive the build's support). Read from the catalog every run.
    // Two gates decide whether a device drives the avatar: Permitted (static) and Present (runtime).

    /// <summary>
    /// What one <see cref="ControlDevice"/> can do: which modes it implements and which scenes it may
    /// drive. Never serialized.
    /// </summary>
    public readonly struct ControlDeviceInfo
    {
        /// <summary> The device this describes. </summary>
        public readonly ControlDevice Device;

        /// <summary> Modes this device implements at all. A mode outside this set is not offered in
        /// the UI and not reachable through the device's own mode enum. </summary>
        public readonly ControlModeMask SupportedModes;

        /// <summary> Whether any of this device's modes needs an on-screen cursor object. </summary>
        public readonly bool NeedsCursor;

        /// <summary> Localization key of the device's player-facing name. </summary>
        public readonly string NameKey;

        /// <summary> Every member at once, in declaration order. </summary>
        public ControlDeviceInfo(ControlDevice device, ControlModeMask supportedModes,
            bool needsCursor, string nameKey)
        {
            Device = device;
            SupportedModes = supportedModes;
            NeedsCursor = needsCursor;
            NameKey = nameKey;
        }

        /// <summary> Whether this device can drive that mode. </summary>
        public bool Supports(ControlMode mode) => (SupportedModes & ToMask(mode)) != 0;

        /// <summary> One mode as the single-bit mask the set is built from. </summary>
        public static ControlModeMask ToMask(ControlMode mode) => mode switch
        {
            ControlMode.Absolute => ControlModeMask.Absolute,
            ControlMode.Relative => ControlModeMask.Relative,
            ControlMode.Direction => ControlModeMask.Direction,
            _ => ControlModeMask.None,
        };
    }
}
