namespace BH.SDK.Rules
{
    // Derived, never stored: a file holds LevelMeta.LevelDifficulty as a plain int. Each value equals
    // the stored number it is read from. Docs/RESOURCE_META.md, "Difficulty".

    /// <summary> How a stored level difficulty reads on the current scale. </summary>
    public enum DifficultyTier : byte
    {
        /// <summary> Not rated; nothing is drawn. </summary>
        Undefined = 0,

        /// <summary> Plays itself, no input needed. </summary>
        Auto = 1,

        /// <summary> The second tier. </summary>
        Drizzle = 2,

        /// <summary> The third tier. </summary>
        Rain = 3,

        /// <summary> The fourth tier. </summary>
        Downpour = 4,

        /// <summary> The fifth tier. </summary>
        Hail = 5,

        /// <summary> The sixth tier. </summary>
        Barrage = 6,

        /// <summary> The seventh tier. </summary>
        Onslaught = 7,

        /// <summary> The eighth tier. </summary>
        Deluge = 8,

        /// <summary> The ninth tier. </summary>
        Inferno = 9,

        /// <summary> The end of this scale and the start of the next. </summary>
        Singularity = 10,
    }
}
