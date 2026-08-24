namespace BH.SDK.Models.Enums.Settings
{
    /// <summary>
    /// Whether the device builds mip-maps for a level's images. They cost a third more memory and
    /// are what stops an image drawn small from shimmering as it moves.
    /// </summary>
    public enum TextureMipmapMode : byte
    {
        /// <summary> Decided per platform. On everywhere today - shimmering is a readability
        /// problem in a game where content moves fast, and a third of a compressed image is
        /// cheap. </summary>
        Auto = 0,

        /// <summary> No mip-maps. Every image samples its full resolution however small it is
        /// drawn, which is sharper when still and crawls when moving. </summary>
        Off = 1,

        /// <summary> Always build them. Images the author marked as pixel art still get none -
        /// smoothing is exactly what that kind exists to refuse. </summary>
        On = 2,
    }
}
