namespace BH.SDK.Models.Enums.Settings
{
    // Zero is not the default: an absent key never overwrites the constructor value (InterfaceSettingsTests).

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