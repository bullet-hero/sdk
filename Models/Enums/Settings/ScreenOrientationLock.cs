namespace BH.SDK.Models.Enums.Settings
{
    // A lock on an AXIS, never a side. Zero is Unlock (follow the device); a level still outranks it
    // (OrientationMath.FromLevel). Lockstep: values 1 and 2 match LevelOrientation. Docs/ENUMS.md

    /// <summary> Which way round the player asked the device to hold this game. </summary>
    public enum ScreenOrientationLock : byte
    {
        /// <summary>
        /// No lock: the device rotates freely and the game follows it. The default.
        /// </summary>
        Unlock = 0,

        /// <summary> Locked to the horizontal axis, either way up. </summary>
        Horizontal = 1,

        /// <summary> Locked to the vertical axis, either way up. </summary>
        Vertical = 2,
    }
}
