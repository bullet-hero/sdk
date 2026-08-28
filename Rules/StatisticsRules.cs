namespace BH.SDK.Rules
{
    // Bounds for the player's own statistics files, in the same "Max" shape KeybindingsRules and
    // PrefabRules use. Nothing here clamps a live counter: these exist so a hand-written or hostile
    // file cannot ask the game to lay out a million rows or preallocate a gigabyte of histogram.

    /// <summary>
    /// Bounds for <see cref="BH.SDK.Models.Statistics.GameStatistics"/> and
    /// <see cref="BH.SDK.Models.Statistics.LevelStatistics"/>.
    /// </summary>
    public static class StatisticsRules
    {
        // A FIXED COUNT RATHER THAN A RESOLUTION, because the histogram has to be comparable across
        // levels of different lengths - a bucket is a percentage of the level, not a number of
        // seconds. 64 puts a three-minute level at about 2.8s per bucket, which is fine enough for
        // an author to see which passage kills people and coarse enough to stay a short array in a
        // file a person may open.
        public const int BucketCount = 64;

        // Comfortably more profiles than a player produces by hand (four life presets x four speed
        // presets x checkpoints x three bots is 96 in theory, but nobody plays the cross product),
        // and low enough that a custom-slider spree cannot grow the file without bound. Eviction is
        // oldest-first by the record's own TimeUtc.
        public const int MaxRecordProfiles = 64;

        /// <summary> How many distinct checkpoints one level may keep a death count for. </summary>
        public const int MaxCheckpointDeaths = 512;

        // Both halves of the cadence live here rather than in the service, because they are what the
        // FILE promises: a crash costs at most FlushSeconds of playtime, and time is never recorded
        // finer than ModelTickSeconds. A reader of the file can state both from these numbers alone.

        /// <summary> How often accumulated time is written into the model, in seconds. </summary>
        public const float ModelTickSeconds = 1f;

        /// <summary> How often a dirty model is forced to disk, in seconds. </summary>
        public const float FlushSeconds = 30f;

        /// <summary> Longest level name snapshot a statistics file may carry. </summary>
        public const int MaxLevelNameLength = 256;

        // A century of continuous play. Not a real limit on anything - it is what stops a corrupted
        // double from rendering as a number nobody can read, and what makes "seconds" a checkable
        // claim rather than an arbitrary float.
        public const double MinSeconds = 0.0;
        public const double MaxSeconds = 60.0 * 60.0 * 24.0 * 365.0 * 100.0;

        public const int MinCount = 0;
    }
}
