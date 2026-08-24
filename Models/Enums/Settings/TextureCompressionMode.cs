namespace BH.SDK.Models.Enums.Settings
{
    /// <summary>
    /// Whether the device packs a level's images into a GPU-compressed format when it loads them.
    /// The player's call, never the author's - it trades image fidelity for memory on THIS device.
    /// </summary>
    public enum TextureCompressionMode : byte
    {
        /// <summary> Decided per platform: on by default where memory is scarce (phones), off where
        /// it is not (desktops). The default, and what almost nobody should change. </summary>
        Auto = 0,

        /// <summary> Never compress. Images keep exactly the pixels their file holds and cost four
        /// bytes each. </summary>
        Off = 1,

        /// <summary> Always compress. Four to eight times less memory per image, at the cost of
        /// block artefacts on gradients and hard edges. Images the author marked as pixel art are
        /// still never compressed - see <see cref="Resources.TextureKind.PixelArt"/>. </summary>
        On = 2,
    }
}
