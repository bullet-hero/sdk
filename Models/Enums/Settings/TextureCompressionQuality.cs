namespace BH.SDK.Models.Enums.Settings
{
    // Encoder effort is a LOAD TIME cost, so the device's budget. Auto derives from TextureKind; the ETC/EAC
    // encoders on mobile ignore it.

    /// <summary> How much time this device spends compressing a level's images. </summary>
    public enum TextureCompressionQuality : byte
    {
        /// <summary> Decided per image from what the author said it is: the careful encoder for
        /// everything except a photograph, which is the kind that survives the fast one. The default.
        /// </summary>
        Auto = 0,

        /// <summary> Always the fast encoder. Levels load sooner; flat areas and gradients pick up
        /// visible blocking. </summary>
        Fast = 1,

        /// <summary> Always the careful encoder - it dithers, which costs load time and never
        /// quality. </summary>
        High = 2,
    }
}
