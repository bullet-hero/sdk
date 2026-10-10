namespace BH.SDK.Models.Enums.Settings
{
    // No version here: it belongs to the display name, a member to what the player CHOSE. None = 0 keeps
    // every consumer additive (a file from before a bot reads as "no bot").

    /// <summary>
    /// Which bot steers the avatar, if any.
    /// </summary>
    public enum BotKind : byte
    {
        /// <summary> Nobody. The player steers, which is the default everywhere. </summary>
        None = 0,

        /// <summary>
        /// Reads the level a fraction of a second ahead every frame and walks towards the room it finds.
        /// </summary>
        Reflex = 1,

        /// <summary>
        /// Plays a route worked out before the run rather than reacting to what it sees. Not
        /// implemented yet: selecting it leaves the avatar to the player.
        /// </summary>
        Warm = 2,
    }
}
