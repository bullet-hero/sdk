namespace BH.SDK.Models.Enums.Settings
{
    // How the GPU samples, and only that: TextureKind.PixelArt still forces Point. Reduction is
    // TextureLoadPlan.BoxDownscale. Why: Docs/ENUMS.md

    /// <summary> How this device samples a level's images when it draws them. </summary>
    public enum TextureFilterMode : byte
    {
        /// <summary> Decided from the rest of the settings: smoothed, and smoothed between mip levels
        /// too when mip-maps are on. The default. </summary>
        Auto = 0,

        /// <summary> No smoothing at all - every texel is a hard square, whatever the image is.
        /// </summary>
        Point = 1,

        /// <summary> Smoothed within one mip level. </summary>
        Bilinear = 2,

        /// <summary> Smoothed within a mip level and between two of them, which is what removes the
        /// visible seam where one takes over from the next. Costs nothing worth measuring, and
        /// nothing at all without mip-maps to blend. </summary>
        Trilinear = 3,
    }
}
