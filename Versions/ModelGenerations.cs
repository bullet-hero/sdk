// ReSharper disable InconsistentNaming

namespace BH.SDK.Versions
{
    // One global counter: a changing domain takes Current + 1 (Docs/VERSIONING.md, "mg").
    // MIRRORED IN BH.SDK.Roslyn's ModelGenerationValues, which cannot reference this assembly:
    // Invalid must agree there.

    /// <summary> Every model generation that exists, by name. </summary>
    public static class ModelGenerations
    {
        /// <summary> No generation at all - an unversioned leaf, or an envelope nothing was read
        /// from. NEGATIVE rather than zero, because zero is a real generation the frozen snapshots
        /// under <c>Versions/V0</c> are written at; every negative value is equally invalid and
        /// this is merely the canonical one. </summary>
        public const int Invalid = -1;

        /// <summary> The scaffold under <c>Versions/V0</c>: shapes that exercise the migration path
        /// end to end, rather than a format any build ever shipped. </summary>
        public const int Test = 0;

        /// <summary> What 1.0.0 shipped. Every live domain but <c>UserSettings</c>,
        /// <c>LevelStatistics</c>, <c>GameStatistics</c> and <c>LevelMeta</c> is still at it. </summary>
        public const int V1_AlphaRelease = 1;

        /// <summary> The first bump after release: <c>UserSettings</c>, <c>LevelStatistics</c>,
        /// <c>GameStatistics</c>, <c>LevelMeta</c> and <c>Collection</c>; what moved in each is listed
        /// in Docs/VERSIONING.md. </summary>
        public const int V2_SimplifyEntrance = 2;

        /// <summary> The newest generation there is - the only thing UI and reports read. A future
        /// bump adds its own named constant and moves this alias onto it. </summary>
        public const int Current = V2_SimplifyEntrance;
    }
}