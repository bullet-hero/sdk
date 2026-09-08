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
        /// Set target framerate to fixed refresh rate.
        /// Can't be lower than 1 and doesn't make sense if higher than screen refresh rate.
        /// Used to low power consumption on phones
        /// </summary>
        Fixed = 2,
    }
}