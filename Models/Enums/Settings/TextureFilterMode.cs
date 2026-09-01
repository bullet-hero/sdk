namespace BH.SDK.Models.Enums.Settings
{
    // How the GPU samples a level's images, and ONLY that. It used to be derived from the author's
    // TextureKind alone (pixel art point-sampled, everything else smoothed), which is right about
    // pixel art and wrong as a general rule: smoothing is what a device does when it draws, not what
    // a picture is, and a player wanting crisp texels everywhere was asking a question nothing here
    // could hear.
    //
    // The kind still WINS where it is making a claim about content: TextureKind.PixelArt forces Point
    // however this is set, exactly as it already forces compression and mip-maps off. A kind
    // restricting the player is the direction this split allows.
    //
    // What this does NOT decide is how an image is REDUCED when it is too large for the device - see
    // TextureLoadPlan.BoxDownscale. Those were one question only while Point meant "pixel art".

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
