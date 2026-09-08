using BH.SDK.Models;

namespace BH.SDK.Generators
{
    // Returned as one value rather than through two separate calls (the old GenerateLevel/
    // GenerateMeta pair), because Level and LevelMeta are two independent serialization roots
    // describing the same thing - a generator that computes them in two passes can quietly let them
    // disagree about, say, how long the level is.

    /// <summary>
    /// Both halves of a newly generated level: the level itself and its separate metadata file.
    /// </summary>
    public readonly struct GeneratedLevel
    {
        /// <summary> The level itself. </summary>
        public readonly Level Level;
        /// <summary> Its metadata, built alongside so the two cannot disagree. </summary>
        public readonly LevelMeta Meta;

        /// <summary> Both halves at once. </summary>
        public GeneratedLevel(Level level, LevelMeta meta)
        {
            Level = level;
            Meta = meta;
        }

        /// <summary> Both halves, for a positional binding. </summary>
        public void Deconstruct(out Level level, out LevelMeta meta)
        {
            level = Level;
            meta = Meta;
        }
    }
}
