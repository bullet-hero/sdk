namespace BH.SDK.Models.Enums.Resources
{
    // What happens outside [0, 1], which is a question a level could always ASK and never answer:
    // TextureResource.TextureResourceUV's tiling half has always reached the shader as _BaseMap_ST,
    // so an author could tile an image by data - and the consumer hard-coded Clamp, so tiling only
    // ever stretched one row of pixels across whatever it was supposed to repeat over.
    //
    // It belongs to the author rather than to the device for the same reason the sub-rect does: it
    // describes the picture's own composition, not what this machine can afford. There is no player
    // setting it composes with, and none would mean anything.

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
