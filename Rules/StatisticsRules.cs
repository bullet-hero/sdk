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
        // A fixed count, not a resolution, so the histogram compares across levels of different length (a
        // bucket is a percentage). 64 puts a three-minute level near 2.8 s per bucket.

        /// <summary> The bucket count, read by DifficultyStatistics, StatisticsModelTests, StatisticsSerializationTests. </summary>
        public const int BucketCount = 64;

        // More profiles than a player produces by hand, low enough that a slider spree cannot grow the
        // file without bound. Eviction is oldest-first by the record's TimeUtc.

        /// <summary> Upper bound of LevelStatistics.Records. </summary>
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

        /// <summary> Lower bound of DeviceTimeStatistics.DeviceGyroSeconds, DeviceTimeStatistics.GamepadSeconds, DeviceTimeStatistics.KeyboardMouseSeconds and 8 more. </summary>
        public const double MinSeconds = 0.0;
        /// <summary> Upper bound of DeviceTimeStatistics.DeviceGyroSeconds, DeviceTimeStatistics.GamepadSeconds, DeviceTimeStatistics.KeyboardMouseSeconds and 8 more. </summary>
        public const double MaxSeconds = 60.0 * 60.0 * 24.0 * 365.0 * 100.0;

        /// <summary> Lower bound of BestRun.Dashes, BestRun.Frame, BestRun.Hits and 34 more. </summary>
        public const int MinCount = 0;
    }
}
