namespace BH.SDK.Models.Enums.Resources
{
    // A second author axis beside TextureKind, undecidable anywhere else; Opaque is an unchecked claim.
    // Why: Docs/ENUMS.md

    /// <summary>
    /// What the level's author states about an image's transparency - the one thing about a picture
    /// that a device can see the file for and still not know.
    /// </summary>
    public enum TextureAlpha : byte
    {
        /// <summary> Not stated: the device decides from the file's own header alone. The default. </summary>
        Auto = 0,

        /// <summary> Fully opaque: alpha is dropped (faster, smaller). Unchecked. </summary>
        Opaque = 1,
    }
}
