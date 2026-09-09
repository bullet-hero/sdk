namespace BH.SDK.Models.Enums.Settings
{
    // NO MAXIMIZED MEMBER, AND THE GAP AT 2 IS DELIBERATE. Unity has one and it was offered here;
    // it earned nothing that Windowed does not already do - the OS maximizes a windowed window on
    // its own, and full screen is what a player who wants the whole display asks for - while
    // costing a THIRD state for every window question to disagree about: the setting said maximized
    // while the window was restored, the stored resolution described a window the OS was sizing,
    // and each apply put the window back where the player had just taken it from. The number 2 is
    // NOT reused, because the mirror below is what makes the conversion trivial, and because a
    // settings file carrying it must keep meaning what it meant.
    //
    // A MIRROR OF UnityEngine.FullScreenMode, VALUE FOR VALUE, and the mirroring is the point: this
    // assembly may not reference UnityEngine (it is meant to run as a server DLL with no engine
    // present), so the engine's enum cannot appear on a settings model. The numbers match so the
    // conversion is trivial, but Core's DisplayModeUtils is still the ONE place that converts - a
    // cast written anywhere else is a second place that has to be kept in step with an enum this
    // project does not own.
    //
    // The default is Windowed, and it moved there together with ProjectSettings.asset's own
    // fullscreenMode: the game may never put ITSELF full screen, and a default of FullScreenWindow
    // was exactly that - a player who never opened this setting was handed a mode nobody picked, on
    // every fresh install. Full screen is reachable from the dropdown and from F11, both of which
    // are the player asking. The member order is the order the modes are OFFERED, which is also
    // Unity's own.
    //
    // Mobile ignores all of this - a phone has one window and it is the screen - which is why the
    // whole Display group is disabled there rather than hidden.

    /// <summary> How the game's window occupies the display. Desktop only. </summary>
    public enum WindowMode : byte
    {
        /// <summary> The game owns the display outright. Lowest latency, slowest to alt-tab. </summary>
        ExclusiveFullScreen = 0,

        /// <summary> A borderless window covering the display. The default. </summary>
        FullScreenWindow = 1,

        /// <summary> A normal, freely resizable window. </summary>
        Windowed = 2,
    }
}
