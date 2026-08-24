namespace BH.SDK.Models.Enums.Settings
{
    // A cap rather than a target: an image already under the limit is never touched, and one over
    // it is halved until it fits. That is what makes this the cheapest lever of the three - memory
    // falls with the SQUARE of the side, so one step down saves more than compression does, and it
    // is also the only one that bounds the worst case a level can inflict on a device at all.

    /// <summary>
    /// The largest side, in pixels, a level's image is allowed to occupy in memory on this device.
    /// Anything bigger is scaled down as it loads; the file on disk is never modified.
    /// </summary>
    public enum TextureSizeLimit : byte
    {
        /// <summary> Decided per platform - 2048 on phones, 4096 on desktops. The default. </summary>
        Auto = 0,

        /// <summary> No cap at all: an image occupies whatever its file holds, up to what the
        /// hardware can address. A single 8192 image costs 256 MB uncompressed. </summary>
        Unlimited = 1,

        /// <summary> At most 1024x1024. </summary>
        Side1024 = 2,

        /// <summary> At most 2048x2048. </summary>
        Side2048 = 3,

        /// <summary> At most 4096x4096. </summary>
        Side4096 = 4,
    }
}
