namespace BH.SDK.Models.Enums.Settings
{
    // THE VALUES ARE Unity's vSyncCount, ON PURPOSE - Off is 0, On is 1, Half is 2 - so the applier
    // is a cast rather than a switch, and a member added here is a number the engine already
    // understands. Off being the zero value is what makes the field additive: a settings file
    // written before it reads back as the behaviour the game already had, which was vsync forced
    // off unconditionally.
    //
    // IT IS DESKTOP-ONLY, like everything else in DisplayGraphicsSettings, and for a stronger reason
    // than the window rows: Unity ignores vSyncCount on Android and iOS entirely - a phone presents
    // on its own refresh and Application.targetFrameRate is the only cap that means anything there.
    //
    // It also OUTRANKS the framerate policy one level up, which is the trap to remember: Unity
    // ignores targetFrameRate whenever vSyncCount is non-zero, so GraphicsSettings.FramerateTarget
    // stops deciding anything the moment this leaves Off. The settings screen disables those rows
    // rather than letting them lie.

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
