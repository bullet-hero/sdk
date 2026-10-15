namespace BH.SDK.Rules
{
    /// <summary> The scale <see cref="BH.SDK.Models.LevelMeta.LevelDifficulty"/> is read on. </summary>
    public static class DifficultyRules
    {
        /// <summary> The stored value of a level nobody rated. </summary>
        public const int Undefined = 0;

        /// <summary> The lowest rated value: the level plays itself. </summary>
        public const int Auto = 1;

        /// <summary> The top of the current scale. </summary>
        public const int ScaleMax = 10;

        // A value above ScaleMax is kept as written and read as the top tier until a later scale exists.

        /// <summary> The tier a stored difficulty reads as. </summary>
        public static DifficultyTier Read(int value)
        {
            if (value <= Undefined) return DifficultyTier.Undefined;
            if (value >= ScaleMax) return DifficultyTier.Singularity;
            return (DifficultyTier)value;
        }
    }
}
