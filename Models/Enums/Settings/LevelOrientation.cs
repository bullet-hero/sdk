namespace BH.SDK.Models.Enums.Settings
{
    // The level's call, outranking the player's setting while it runs. NotSpecified is an absence of opinion,
    // not Unlock; values 1 and 2 line up with it (OrientationMath.FromLevel). Zero is not the default,
    // Horizontal is: older levels read back as played. Docs/ENUMS.md

    /// <summary> The orientation a level is composed for, and plays in whatever the player set. </summary>
    public enum LevelOrientation : byte
    {
        /// <summary>
        /// The author asserts the level reads correctly in EITHER orientation; the editor warns about it.
        /// </summary>
        NotSpecified = 0,

        /// <summary> Composed for a horizontal frame. The default. </summary>
        Horizontal = 1,

        /// <summary> Composed for a vertical frame. </summary>
        Vertical = 2,
    }
}
