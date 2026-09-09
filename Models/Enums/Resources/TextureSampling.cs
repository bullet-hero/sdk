namespace BH.SDK.Models.Enums.Resources
{
    // SHARP OR SOFT IS A LOOK, NOT A BUDGET, and that is why this axis is the author's even though
    // its neighbour in the player's settings (TextureFilterMode) looks like the same question. Point
    // and bilinear sampling cost the same on every GPU this ships to, so nothing about the device
    // decides between them; what they change is whether the picture has visible pixels, which is
    // exactly the kind of statement the rest of TextureResource's fields make.
    //
    // It was DERIVED from TextureKind before it existed - pixel art point-sampled, everything else
    // smoothed - and that derivation is what this replaces. A kind is a claim about content and it
    // still SEEDS this one (PixelArt seeds Sharp), but "crisp" and "pixel art" are not the same
    // claim: a hand-drawn sprite sheet an author wants unblurred is a Graphic, and saying PixelArt to
    // get the look also refuses compression and mip-maps, which is a memory decision made by
    // accident.
    //
    // WHAT IT DOES NOT TOUCH IS RESAMPLING. Refusing to be AVERAGED while being scaled down is a
    // property of pixel art alone and stays keyed to the kind (see TextureLoadPlan's own header): a
    // sharp graphic reduced from 4096 to 1024 must still average, or fifteen pixels in sixteen are
    // thrown away and the mip chain cannot repair it. Sharp says how the finished texture is DRAWN.
    //
    // The player's own Filtering keeps a real say: it decides whether a smoothed image blends
    // between mip levels (bilinear against trilinear), which IS a device question. So the two halves
    // compose rather than compete - this one picks sharp or soft, that one picks how soft.

    /// <summary>
    /// How an image is sampled when it is drawn - whether its pixels are visible or blurred
    /// together. Artistic: the same on every device, and the author's to state.
    /// </summary>
    public enum TextureSampling : byte
    {
        /// <summary>
        /// Not stated. Follows what the picture is - pixel art is drawn sharp, everything else is
        /// smoothed - and then the player's own filtering setting. The default.
        /// </summary>
        Auto = 0,

        /// <summary>
        /// Blurred: neighbouring pixels blend, so the image has no visible grid however far it is
        /// scaled up. What every kind but pixel art does by default.
        /// </summary>
        Smooth = 1,

        /// <summary>
        /// Crisp: every pixel is drawn as a hard square, so scaling up shows the grid. What pixel art
        /// needs, and available to any image that wants the look without claiming to be one.
        /// </summary>
        Sharp = 2,
    }
}
