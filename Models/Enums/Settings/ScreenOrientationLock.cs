namespace BH.SDK.Models.Enums.Settings
{
    // A LOCK ON AN AXIS, NEVER ON A SIDE, and the name says so on purpose. Horizontal permits both
    // landscape orientations and Vertical both portrait ones - a player who turns the phone over
    // keeps playing, which is the whole point of the feature. Anything that resolves this to a
    // single side has misread it.
    //
    // THE ZERO VALUE IS THE DEFAULT AGAIN, AND THAT IS A REVERSAL. It was Horizontal, on the
    // argument that Unlock would opt every player into free rotation on a UI with no portrait
    // layout - true when it was written, and no longer: the panel root carries `.portrait` /
    // `.landscape` off the MEASURED aspect and screens lay themselves out from it. A phone that a
    // player turns over should follow, which is what this setting is for, so the shipped answer is
    // now "follow the device" and a lock is something they ask for.
    //
    // A LEVEL STILL OUTRANKS IT while that level is running (OrientationMath.FromLevel), and
    // LevelOrientation's own default is still Horizontal - so this frees the menu, the browser and
    // every settings screen, and leaves a level holding whatever axis it declares. That is the whole
    // of the change: the editor is locked by its context and answers to neither.
    //
    // Nothing needs migrating either way (Rule 11), and the zero value costs nothing here: an absent
    // JSON key is never written, so the constructor's value survives deserialization untouched.
    //
    // Its values line up with LevelOrientation's at 1 and 2 BY DESIGN, not by accident, and
    // OrientationMath.FromLevel is the one place that converts between them - the same discipline
    // ControlMode and the per-device control modes already keep, for the same reason.

    /// <summary> Which way round the player asked the device to hold this game. </summary>
    public enum ScreenOrientationLock : byte
    {
        /// <summary>
        /// No lock: the device rotates freely and the game follows it. Identical to what the
        /// project ships in its own player settings, so this is the honest name for the game's
        /// behaviour before anything managed it - and the default.
        /// </summary>
        Unlock = 0,

        /// <summary> Locked to the horizontal axis, either way up. </summary>
        Horizontal = 1,

        /// <summary> Locked to the vertical axis, either way up. </summary>
        Vertical = 2,
    }
}
