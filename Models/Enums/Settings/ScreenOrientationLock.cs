namespace BH.SDK.Models.Enums.Settings
{
    // A LOCK ON AN AXIS, NEVER ON A SIDE, and the name says so on purpose. Horizontal permits both
    // landscape orientations and Vertical both portrait ones - a player who turns the phone over
    // keeps playing, which is the whole point of the feature. Anything that resolves this to a
    // single side has misread it.
    //
    // THE ZERO VALUE IS NOT THE DEFAULT - Horizontal is - which is the same call MenuBackgroundKind
    // made and it costs the same nothing: an absent JSON key is never written, so InterfaceSettings'
    // constructor value survives deserialization untouched and no older settings file can read back
    // as Unlock. That is also what makes the default SAFE rather than merely additive, since Unlock
    // would have opted every existing player into free rotation on a UI with no portrait layout.
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
        /// behaviour before anything managed it.
        /// </summary>
        Unlock = 0,

        /// <summary> Locked to the horizontal axis, either way up. The default. </summary>
        Horizontal = 1,

        /// <summary> Locked to the vertical axis, either way up. </summary>
        Vertical = 2,
    }
}
