namespace BH.SDK.Models.Enums.Settings
{
    /// <summary>
    /// Which anti-aliasing method the device renders with. One choice rather than a set - the two
    /// real options spend their cost in different places, so running both buys nothing.
    /// </summary>
    public enum AntiAliasingType : byte
    {
        /// <summary> No anti-aliasing at all. </summary>
        None = 0,

        /// <summary>
        /// Multisampling, the default: every shape edge is real geometry, so it resolves what aliases; cost
        /// grows with transparent overdraw.
        /// </summary>
        Msaa = 1,

        /// <summary>
        /// Fast approximate AA: one full-screen pass whose cost ignores overdraw. Blurs thin outlines.
        /// </summary>
        Fxaa = 2,
    }
}
