namespace BH.SDK.Models.Enums.Settings
{
    // No Maximized member; the gap at 2 is never reused. Mirrors UnityEngine.FullScreenMode value for value
    // (no UnityEngine here); only Core's DisplayModeUtils converts. Docs/ENUMS.md

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
