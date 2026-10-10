namespace BH.SDK.Models.Enums.Resources
{
    // Sharp or soft is a look, not a budget, so it is the author's. A kind SEEDS it; resampling stays keyed
    // to the kind (PixelArt), not to this. Why: Docs/ENUMS.md

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
