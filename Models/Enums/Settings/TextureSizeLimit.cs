namespace BH.SDK.Models.Enums.Settings
{
    // A cap: an image over it is halved until it fits; memory falls with the SQUARE of the side.
    // Numbers are appended, not in ladder order: present via an explicit array, never Enum.GetValues.

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