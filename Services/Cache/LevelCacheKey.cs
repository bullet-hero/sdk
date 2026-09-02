using System;

namespace BH.SDK.Services.Cache
{
    // =========================================================================================
    // DEAD CODE, KEPT ON PURPOSE - A DELETION CANDIDATE.
    //
    // Nothing constructs, registers or calls this. The level cache is disconnected from the game:
    // `RootScope` does not register `LevelCacheService`, and `LevelLoaderService` no longer hooks
    // it on any read, write or delete. It is left in the tree for ONE reason - the session that
    // builds the `.blob` format is meant to read it before deleting it, because it is a worked
    // example of what a hand-written codec for this model looks like and which of its types are
    // polymorphic. See docs/issues/ROSLYN_PLAN.md, and LOADING_HISTORY.md section 10.
    //
    // WHY IT IS GOING RATHER THAN BEING FIXED: everything here is what a fast FORMAT does, plus
    // the cost of deciding when it is stale, an invalidation hook on every write path, and a
    // second hand-written serializer to keep in step with the real one. A format has none of
    // those problems, because it cannot disagree with the file - it IS the file.
    //
    // DO NOT WIRE IT BACK UP to make something faster. If loading is slow, that is the format's
    // job now.
    // =========================================================================================

    // WHAT MAKES A CACHED LEVEL THE SAME LEVEL, and every part of it is here rather than agreed
    // between the writer and the reader. A cache whose staleness rule lives at its call sites is a
    // cache that is stale exactly where somebody forgot it.
    //
    // FOUR PARTS, and each answers a different way of going wrong:
    //
    // - `Name` is which document this is - the store-relative path of the source. Two levels in one
    //   store must not answer for each other.
    // - `Length` and `Stamp` are whether that document has changed since. Length alone misses an
    //   edit that kept the size; a timestamp alone misses a tool that preserves timestamps, which
    //   is exactly what a copy, a restore from a backup and an unzip all do. Together they miss
    //   only an edit that changed neither, which is a file written to be indistinguishable.
    // - `Format` is whether THIS LIBRARY still reads what it wrote. It is bumped by hand whenever
    //   the codec changes, and it is the reason a model change cannot quietly resurrect a cache
    //   that decodes into the wrong shape.
    //
    // NOTHING HERE IS A HASH OF THE CONTENT. Hashing 16 MB to decide whether to skip reading 16 MB
    // gives most of the cost back, and the pair above is what every build system in existence uses
    // for the same reason.
    //
    // A MISMATCH IS ALWAYS A MISS, NEVER AN ERROR. Every field exists to make the cache decline;
    // declining costs one ordinary load, and that is the whole safety story of this feature.

    /// <summary> Identifies one cached level, and decides when the cache no longer answers for it.
    /// </summary>
    public readonly struct LevelCacheKey : IEquatable<LevelCacheKey>
    {
        /// <summary> The source document this was parsed from, as its store names it. </summary>
        public readonly string Name;

        /// <summary> Its length in bytes. </summary>
        public readonly long Length;

        /// <summary> Whatever the host uses for "changed since" - a write time in ticks, an ETag's
        /// hash, a revision number. The cache only ever compares it. </summary>
        public readonly long Stamp;

        /// <summary> The codec version that wrote it, i.e. <see cref="LevelCacheFormat.Version"/>.
        /// </summary>
        public readonly int Format;

        public LevelCacheKey(string name, long length, long stamp, int format = LevelCacheFormat.Version)
        {
            Name = name ?? string.Empty;
            Length = length;
            Stamp = stamp;
            Format = format;
        }

        /// <summary> False for a key that names nothing, which is what a caller with no source
        /// document has - a generated level, a level being imported. Those are never cached. </summary>
        public bool IsValid => !string.IsNullOrEmpty(Name) && Length > 0;

        public bool Equals(LevelCacheKey other)
            => Length == other.Length && Stamp == other.Stamp && Format == other.Format
               && string.Equals(Name, other.Name, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is LevelCacheKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Name, Length, Stamp, Format);

        public override string ToString() => $"{Name} ({Length} B, stamp {Stamp}, v{Format})";
    }
}
