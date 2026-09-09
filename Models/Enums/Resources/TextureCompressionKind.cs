namespace BH.SDK.Models.Enums.Resources
{
    // THE VETO HALF OF TextureKind, SPLIT OUT. A kind used to carry two unrelated statements at once:
    // what the picture is, and whether a device may pack it into a block format. PixelArt and
    // Gradient refused compression, every other kind allowed it, and there was no way to say either
    // thing on its own - so an author with a photograph carrying one precious gradient could only
    // protect it by calling it a Gradient, which is a lie the rest of the pipeline then reads, and an
    // author whose pixel-art tile was perfectly happy compressed had no way to give the memory back.
    //
    // A kind still SEEDS this axis, which is what keeps every level authored before it behaving
    // identically: Auto means "whatever the picture's kind implies", i.e. PixelArt and Gradient
    // refuse and everything else allows. Only an explicit value here overrides that.
    //
    // WHAT IT CANNOT TOUCH IS THE SIZE CAP, like every other author axis. Refuse quadruples what an
    // image costs on a phone, exactly as Gradient already could, and the cap is what still bounds the
    // worst case a careless level can inflict on a device - see TextureLoadPlanner's header.
    //
    // Mip-maps are deliberately NOT on this axis. Refusing them is not "preserve the image": it
    // trades aliasing when the image is drawn small for memory, which is the opposite trade, and
    // pixel art refuses them for its own reason (a lower mip is a blurred one, which is what the kind
    // exists to forbid). That veto stays with the kind.

    /// <summary>
    /// Whether a device may pack this image into a lossy GPU-compressed format. The author's
    /// permission; how hard, and whether the device wants to at all, stays the player's.
    /// </summary>
    public enum TextureCompressionKind : byte
    {
        /// <summary>
        /// Not stated. Follows what the picture is - pixel art and gradients refuse, everything else
        /// allows - and then the player's own compression setting. The default.
        /// </summary>
        Auto = 0,

        /// <summary>
        /// Compressible: the picture survives a block format well enough, whatever its kind implies.
        /// The device still decides whether to.
        /// </summary>
        Allow = 1,

        /// <summary>
        /// Never compressed, however the player set it. For content a 4x4 block format visibly
        /// damages - a soft ramp that bands, an edge that smears - and it is paid for in memory.
        /// </summary>
        Refuse = 2,
    }
}
