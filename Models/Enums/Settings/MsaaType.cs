namespace BH.SDK.Models.Enums.Settings
{
    // The value IS the sample count, so nothing has to translate it - except None, which is 0 here
    // and 1 in every graphics API, since one sample per pixel is what "no multisampling" means.
    // Convert with MsaaTypeExtensions.ToSampleCount rather than casting.

    /// <summary> How many samples <see cref="AntiAliasingType.Msaa"/> takes per pixel. </summary>
    public enum MsaaType : byte
    {
        /// <summary> No multisampling. Deliberately not offered by the settings UI - turning MSAA
        /// off is what <see cref="AntiAliasingType.None"/> is for - but a hand-edited file may hold
        /// it and it means exactly what it says. </summary>
        None = 0,

        /// <summary> Two samples. The default, and where a phone should stay. </summary>
        X2 = 2,

        /// <summary> Four samples. What a desktop can afford. </summary>
        X4 = 4,

        /// <summary> Eight samples. Rarely worth it, and a device that does not support it silently
        /// resolves to the highest count it has. </summary>
        X8 = 8,
    }
}
