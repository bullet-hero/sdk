namespace BH.SDK.Models.Enums.Meta
{
    // The numeric value IS the minimum age. Unrated is 0, so an unfilled record never claims adult content;
    // check for it explicitly, it is not "safe for everyone". One scale, not per-board: Docs/ENUMS.md

    /// <summary> Minimum age a piece of content is authored for. </summary>
    public enum AgeRating : byte
    {
        /// <summary> Nothing was declared. Not a claim that the content is safe. </summary>
        Unrated = 0,

        /// <summary> Suitable for everyone (PEGI 3 / ESRB Everyone). </summary>
        Everyone = 3,
        Age6 = 6,
        Age12 = 12,
        Age16 = 16,
        Age18 = 18,
    }
}
