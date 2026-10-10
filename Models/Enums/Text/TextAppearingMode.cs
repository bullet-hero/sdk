namespace BH.SDK.Models.Enums.Text
{
    // Which characters hide behind the mask; the fraction hidden is keyframed. Random is zero: text
    // resolving out of noise rather than a wipe.
    /// The order a text object's characters come out from behind its appearing mask.
    /// </summary>
    public enum TextAppearingMode : byte
    {
        /// <summary>Scattered, but the same scatter every run - a character hidden at a lower
        /// fraction stays hidden at a higher one, so the text resolves instead of flickering.</summary>
        Random = 0,

        /// <summary>Reveals from the first character onward, so the hidden ones trail at the end.</summary>
        Forward = 1,

        /// <summary>Reveals from the last character backward, so the hidden ones lead at the start.</summary>
        Backward = 2,
    }
}
