namespace BH.SDK.Versions
{
    // THE GENERATIONS THEMSELVES, NAMED. A generation is a plain int, so without this every
    // [ModelGeneration] would carry a bare digit and the day a domain moves nobody could tell which
    // digits mean the same thing. A generation is assigned FORWARD and never reused: a domain takes
    // the next free number, so the twenty domains are free to diverge and only the one that changed
    // needs a snapshot and a migrator.
    //
    // MIRRORED IN BH.SDK.Roslyn's ModelGenerationValues, which cannot reference this assembly at
    // all - the generator only sees the user's source through symbols. Invalid must agree there.

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

        /// <summary> What the game writes today. Every live domain is at this one. </summary>
        public const int Release = 1;

        /// <summary> The newest generation there is - the only thing UI and reports read. A future
        /// bump adds its own named constant and moves this alias onto it. </summary>
        public const int Current = Release;
    }
}
