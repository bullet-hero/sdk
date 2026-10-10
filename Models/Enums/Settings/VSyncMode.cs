namespace BH.SDK.Models.Enums.Settings
{
    // Values are Unity's vSyncCount (Off = 0), so the applier is a cast. Desktop-only, and non-zero OUTRANKS
    // targetFrameRate (the settings screen disables those rows). Docs/ENUMS.md

    /// <summary> Whether presentation waits for the display's refresh, and for how many of them. </summary>
    public enum VSyncMode : byte
    {
        /// <summary> Present as soon as a frame is ready; the framerate cap is what limits it. </summary>
        Off = 0,

        /// <summary> One frame per refresh - no tearing, at one refresh of latency. </summary>
        On = 1,

        /// <summary> One frame per second refresh: half the display's rate, steady. </summary>
        Half = 2,
    }
}
