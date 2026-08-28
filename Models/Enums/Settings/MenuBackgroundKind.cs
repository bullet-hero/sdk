namespace BH.SDK.Models.Enums.Settings
{
    // THE ZERO VALUE IS NOT THE DEFAULT, which is the opposite call from BotKind's and costs nothing.
    // The members are ordered the way the three modes are OFFERED to the player, and the migration
    // this looks like it needs does not exist: an absent JSON key is never written, so
    // InterfaceSettings' constructor value survives deserialization untouched and no older file can
    // read back as None. That is the same mechanism OpenMenuOnLose already relies on, and
    // InterfaceSettingsTests pins it - so this stays a free choice rather than a trade someone has to
    // remember.

    /// <summary> What the main menu draws behind its buttons. </summary>
    public enum MenuBackgroundKind : byte
    {
        /// <summary> Nothing. The cheapest menu there is, and the one a weak device wants. </summary>
        None = 0,

        /// <summary>
        /// A field of rotating shapes - also the project's standing overdraw stress scene, which is
        /// why it is worth keeping reachable after it stopped being the default.
        /// </summary>
        Shapes = 1,

        /// <summary>
        /// A live arena: the reflex bot playing against hazards emitted as the menu runs. The
        /// default.
        /// </summary>
        Bot = 2,
    }
}