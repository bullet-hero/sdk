namespace BH.SDK.Models.Enums.Settings
{
    /// <summary> Which framerate policy applies, globally or to one level. </summary>
    public enum FramerateTarget
    {
        /// <summary>
        /// If setup for local - take global value.
        /// If setup for global - take ScreenHZ
        /// </summary>
        Default = 0,
        
        /// <summary>
        /// Set target framerate to refresh rate of screen
        /// (may work incorrectly in between screens, 1=165hz, 2=60hz, result=?)
        /// </summary>
        ScreenHz = 1,
        
        /// <summary>
        /// Fixed to the refresh rate: not lower than 1, pointless above the screen's. Saves power on phones.
        /// </summary>
        Fixed = 2,
    }
}