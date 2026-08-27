namespace BH.SDK.Models.Enums.Settings
{
    // THE VERSION IS DELIBERATELY NOT IN HERE. A bot's version is what its display name carries
    // ("Reflex Bot v1"), because a version is a statement about the current implementation while a
    // member of this enum is a statement about what the player CHOSE - and those two have to be able
    // to move independently. Shipping Reflex v2 must not silently invalidate every settings file
    // that already says `Reflex`, and it would if the member were named for the version.
    //
    // None = 0 is what makes every consumer additive: a settings file written before a bot existed
    // reads back as "no bot" without a migration, and a launch that says nothing gets the player's
    // own hands.

    /// <summary>
    /// Which bot steers the avatar, if any.
    /// </summary>
    public enum BotKind : byte
    {
        /// <summary> Nobody. The player steers, which is the default everywhere. </summary>
        None = 0,

        /// <summary>
        /// Reads the level a fraction of a second ahead every frame and walks towards whatever room
        /// it can find. Knows nothing about the level before it starts, so it costs frame time for as
        /// long as it runs and can still be cornered.
        /// </summary>
        Reflex = 1,

        /// <summary>
        /// Plays a route worked out before the run rather than reacting to what it sees. Not
        /// implemented yet: selecting it leaves the avatar to the player.
        /// </summary>
        Warm = 2,
    }
}
