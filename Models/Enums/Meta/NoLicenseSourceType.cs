namespace BH.SDK.Models.Enums.Meta
{
    // Where a work with no stated terms came from: the author's own statement, stored in the level.
    // An enum, not a TrustedSource key. Why: Docs/ENUMS.md

    /// <summary> Which platform an unlicensed work was taken from. </summary>
    public enum NoLicenseSourceType : byte
    {
        /// <summary> Origin not stated. </summary>
        Undefined = 0,

        YouTube = 1,
        SoundCloud = 2,
        Spotify = 3,
    }
}
