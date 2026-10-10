using BH.SDK.Models;

namespace BH.SDK.Generators
{
    // One value, not two calls: Level and LevelMeta are independent roots describing one thing, and
    // two passes can disagree about, say, its length.

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
