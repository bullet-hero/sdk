namespace BH.SDK.Models.Enums.Resources
{
    // States what the picture IS, never what the device should do; the player's settings meet it in Core's
    // TextureLoadPlanner. A kind SEEDS TextureSampling and TextureCompressionKind (their Auto reads it).

    /// <summary>
    /// What an image IS, as the level's author sees it. Purely artistic - it names the content, and
    /// the player's own graphics settings decide what the device does with content of that kind.
    /// </summary>
    public enum TextureKind : byte
    {
        /// <summary> Not stated: the device decides, as cautiously as for <see cref="Graphic"/>. </summary>
        Auto = 0,

        /// <summary>
        /// A photograph or painted backdrop: continuous tone, survives resampling and compression best.
        /// </summary>
        Photo = 1,

        /// <summary>
        /// A drawing, logo or icon: flat areas with hard edges, so a device compresses it more carefully.
        /// </summary>
        Graphic = 2,

        /// <summary>
        /// Pixel art: every pixel is authored. Seeds Sharp sampling and a compression refusal, is never
        /// mip-mapped, and is never AVERAGED when scaled down.
        /// </summary>
        PixelArt = 3,

        /// <summary>
        /// A smooth gradient or glow: block compression bands a ramp, so it seeds a compression refusal.
        /// </summary>
        Gradient = 4,
    }
}