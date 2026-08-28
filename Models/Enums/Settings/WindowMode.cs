namespace BH.SDK.Models.Enums.Settings
{
    // A MIRROR OF UnityEngine.FullScreenMode, VALUE FOR VALUE, and the mirroring is the point: this
    // assembly may not reference UnityEngine (it is meant to run as a server DLL with no engine
    // present), so the engine's enum cannot appear on a settings model. The numbers match so the
    // conversion is trivial, but Core's DisplayModeUtils is still the ONE place that converts - a
    // cast written anywhere else is a second place that has to be kept in step with an enum this
    // project does not own.
    //
    // The default is FullScreenWindow because that is what ProjectSettings.asset already ships
    // (fullscreenMode: 1), so a player who never opens this setting keeps exactly the window they
    // have today. The member order is the order the modes are OFFERED, which is also Unity's own.
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

        /// <summary> A normal window, maximised. </summary>
        MaximizedWindow = 2,

        /// <summary> A normal, freely resizable window. </summary>
        Windowed = 3,
    }
}
