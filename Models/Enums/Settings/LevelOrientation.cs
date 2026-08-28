namespace BH.SDK.Models.Enums.Settings
{
    // WHICH WAY THIS LEVEL IS HELD, decided by its author and outranking the player's own setting
    // for as long as the level is running. A level is a composition inside a frame, and the frame's
    // shape is as much a part of the authoring as the content in it - so this is the level's call,
    // not the device's.
    //
    // NotSpecified is NOT the same statement as ScreenOrientationLock.Unlock, which is why these are
    // two types rather than one. Unlock is a choice a player makes and a real resolved answer; this
    // is an ABSENCE of opinion that resolves to somebody else's answer and can never itself be one.
    // Merged, a single member would have to mean both, and the difference between them is the whole
    // content of the resolution ladder. Values 1 and 2 line up with that enum's on purpose;
    // OrientationMath.FromLevel is the one place that converts.
    //
    // THE ZERO VALUE IS NOT THE DEFAULT - Horizontal is - and here that is load-bearing rather than
    // tidy. Every level authored before this field existed reads back as Horizontal, which is how
    // the game already played it, so no level is silently opted into a portrait screen its content
    // was never composed for and nothing needs migrating. A NotSpecified default would have done the
    // opposite to every level that exists.

    /// <summary> The orientation a level is composed for, and plays in whatever the player set. </summary>
    public enum LevelOrientation : byte
    {
        /// <summary>
        /// The author asserts this level reads correctly in EITHER orientation, so it plays in
        /// whichever one the player has. A claim about the level's own layout, which is why the
        /// editor warns about it rather than merely offering it.
        /// </summary>
        NotSpecified = 0,

        /// <summary> Composed for a horizontal frame. The default. </summary>
        Horizontal = 1,

        /// <summary> Composed for a vertical frame. </summary>
        Vertical = 2,
    }
}
