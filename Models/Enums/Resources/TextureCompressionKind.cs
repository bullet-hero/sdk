namespace BH.SDK.Models.Enums.Resources
{
    // The veto half of TextureKind as its own axis; Auto means what the kind implies. It cannot touch the
    // size cap, and mip-maps stay with the kind. Why: Docs/ENUMS.md

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
