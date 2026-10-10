namespace BH.SDK.Models.Enums.Resources
{
    // The author's, like the sub-rect: it describes the picture's composition and no player setting composes
    // with it. Docs/ENUMS.md

    /// <summary> How an image continues past its own edges. </summary>
    public enum TextureWrapKind : byte
    {
        /// <summary> The edge pixels stretch outwards forever. The default, and what every image did
        /// before this existed. </summary>
        Clamp = 0,

        /// <summary> The image repeats, so a tiling UV lays out a grid of copies. </summary>
        Repeat = 1,

        /// <summary> The image repeats flipped each time, so copies meet edge to edge with no seam.
        /// </summary>
        Mirror = 2,
    }
}
