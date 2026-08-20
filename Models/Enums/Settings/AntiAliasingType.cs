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
        /// Multisampling. The default, and the method this game's content is shaped for: every shape
        /// edge is real geometry, so MSAA resolves exactly what aliases, at full sharpness, on every
        /// platform. Its cost grows with transparent overdraw (blending resolves per sample), which
        /// is what the sample count exists to trade against.
        /// </summary>
        Msaa = 1,

        /// <summary>
        /// Fast approximate AA - one full-screen pass whose cost does NOT depend on how much the
        /// level overdraws, which is the only reason to pick it over MSAA on a weak device. It blurs
        /// thin outlines and crawls on movement, so it is an option rather than a default.
        /// </summary>
        Fxaa = 2,
    }
}
