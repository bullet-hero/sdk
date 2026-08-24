using System;
using System.IO.Compression;
using System.Text;

namespace BH.SDK.Services.Archive
{
    // EVERY FIELD HERE EXISTS TO MAKE TWO PACKS OF THE SAME CONTENT BYTE-IDENTICAL, and that is not
    // neatness: the backend will want to recognise a re-upload of a package it already holds by its
    // digest, and a timestamp taken from the clock defeats that on the first try. So a tar entry
    // carries a pinned time, no owner, no group, and one fixed mode - none of which any consumer of
    // a level package has a use for, and all of which vary per machine if left to the file system.
    //
    // The claim it supports is exactly "the same input, packed twice by the same build, is the same
    // bytes". Deflate's own output across runtimes is not something this can promise, and nothing
    // here depends on it: the digest is computed on whatever was actually written.
    //
    // NAMES ARE CAPPED AT 100 BYTES, which is shorter than ustar's own 100 + 155 prefix form. That
    // is SharpZipLib's limit rather than the format's: its TarHeader writes the name field and has
    // no way to fill the prefix, so a longer name would be silently TRUNCATED into a different file
    // on the way out. A caller renames what does not fit and says so - see LevelPackageBuilder.

    /// <summary> How an archive is written, and what makes writing it reproducible. </summary>
    public sealed class ArchivePolicy
    {
        /// <summary> What a pack uses when the caller states nothing of its own. </summary>
        public static ArchivePolicy Default { get; } = new ArchivePolicy();

        // 1980-01-01 UTC rather than the Unix epoch: a zero mtime reads as "unset" to some tools and
        // shows up as 1970 in every listing, while this is the same instant ZIP has used as its own
        // floor for forty years and no reader treats it as missing.

        /// <summary> The instant every entry is stamped with. </summary>
        public static readonly DateTime PinnedModTime = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary> Unix permission bits every entry carries - rw-r--r--. </summary>
        public const int FileMode = 420;

        /// <summary> The longest entry name that survives a round trip through this writer. </summary>
        public const int MaxNameBytes = 100;

        /// <summary> Entry names are UTF-8, which is what every modern tar reader assumes. </summary>
        public static readonly Encoding NameEncoding = new UTF8Encoding(false);

        /// <summary> How hard gzip works. Optimal, since a package is written once and read often -
        /// and since netstandard2.1 has no SmallestSize to reach for. </summary>
        public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Optimal;

        /// <summary> Whether an entry name fits what the writer can actually record. </summary>
        public static bool FitsName(string path) =>
            !string.IsNullOrEmpty(path) && NameEncoding.GetByteCount(path) <= MaxNameBytes;
    }
}
