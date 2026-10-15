using BH.SDK.Models.Device;

namespace BH.SDK.Utils.Performance
{
    /// <summary> The devices the author measured the stress test on, standing in for a device that has not
    /// run it. </summary>
    public enum ReferenceDevice : byte
    {
        /// <summary> A weak Android phone. </summary>
        WeakAndroid = 0,

        /// <summary> A mid-range Android phone; iOS reads it until an iOS device is measured. </summary>
        MidAndroid = 1,

        /// <summary> A desktop PC with a discrete GPU. </summary>
        Pc = 2,

        /// <summary> A laptop on its integrated GPU. </summary>
        LaptopIGpu = 3,
    }

    // Filled from the stress test logs of the reference devices (LEVEL_PERFORMANCE_RATING_PLAN, phase 4);
    // the measurements and their dates: the client's Docs/Issues/LEVEL_PERFORMANCE_HISTORY.md.

    /// <summary> The shipped profile of each reference device, the same constants the server scores with.
    /// Null while a device has not been measured, which scores as unknown rather than as a guess. </summary>
    public static class ReferenceDeviceProfiles
    {
        /// <summary> Every reference device, in enum order. </summary>
        public static readonly ReferenceDevice[] All =
        {
            ReferenceDevice.WeakAndroid, ReferenceDevice.MidAndroid, ReferenceDevice.Pc, ReferenceDevice.LaptopIGpu,
        };

        /// <summary> A fresh copy of the profile measured on <paramref name="device"/>, or null when it has
        /// not been measured yet. </summary>
        public static DeviceProfile For(ReferenceDevice device) => device switch
        {
            _ => null,
        };
    }
}
