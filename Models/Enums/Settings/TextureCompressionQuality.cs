namespace BH.SDK.Models.Enums.Settings
{
    // How hard the encoder works, which is a LOAD TIME cost and therefore the device's own budget.
    // It used to be derived from the author's TextureKind (everything but a photo took the careful
    // encoder), and that was the wrong half: the resulting image is identical in size and format
    // either way, so nothing about it is a property of the picture - only of how long the player
    // waited for it.
    //
    // Auto keeps that derivation exactly, so a player who never opens this sees no change at all.
    //
    // Ignored by the ETC/EAC encoders mobile platforms use, which take no such parameter - it is a
    // desktop BC1/BC3 setting in practice.

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
