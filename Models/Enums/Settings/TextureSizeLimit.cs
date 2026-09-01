namespace BH.SDK.Models.Enums.Settings
{
    // A cap rather than a target: an image already under the limit is never touched, and one over
    // it is halved until it fits. That is what makes this the cheapest lever of the three - memory
    // falls with the SQUARE of the side, so one step down saves more than compression does, and it
    // is also the only one that bounds the worst case a level can inflict on a device at all.
    //
    // A MEMBER'S NUMBER IS WHAT A SETTINGS FILE STORES, so a rung is APPENDED and never renumbered -
    // the rule RandomTracks states for its own track ids. The numbering is therefore not the ladder's
    // order, and nothing may present these by Enum.GetValues: the settings screen hands
    // LocalizedEnumDropdown an explicit ordered array, which is also what its index-based read back
    // depends on.

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

        /// <summary> At most 512x512. The floor of the ladder, for a phone that would otherwise
        /// spend most of its budget on one backdrop. </summary>
        Side512 = 5,

        /// <summary> At most 8192x8192. Effectively uncapped on today's content, but stated rather
        /// than <see cref="Unlimited"/> so a hostile file still meets a number. </summary>
        Side8192 = 6,
    }
}