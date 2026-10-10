using System;
using System.IO.Compression;
using System.Text;

namespace BH.SDK.Services.Archive
{
    // Every field exists so two packs of the same content are byte-identical (a backend recognises a
    // re-upload by digest). Name cap: 100 bytes on write, for every format, because SharpZipLib would
    // truncate a longer tar name; a profile alone may exceed it. Reasons: Docs/ARCHIVE.md.

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

        /// <summary> The longest entry name any format here writes - the tightest of them sets it for all. </summary>
        public const int MaxEntryNameBytes = 100;

        /// <summary> Entry names are UTF-8, which is what every modern tar reader assumes. </summary>
        public static readonly Encoding NameEncoding = new UTF8Encoding(false);

        /// <summary> How hard gzip works. Optimal, since an archive is written once and read often -
        /// and since netstandard2.1 has no SmallestSize to reach for. </summary>
        public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Optimal;

        /// <summary> The longest entry name THIS policy writes. Only zip honours a value above
        /// <see cref="MaxEntryNameBytes"/>; tar refuses such a policy outright. </summary>
        public int MaxNameBytes { get; set; } = MaxEntryNameBytes;

        /// <summary> Whether an entry name fits what the writer can actually record. </summary>
        public static bool FitsName(string path) =>
            !string.IsNullOrEmpty(path) && NameEncoding.GetByteCount(path) <= MaxEntryNameBytes;

        /// <summary> Whether an entry name fits this policy's own cap. </summary>
        public bool Fits(string path) =>
            !string.IsNullOrEmpty(path) && NameEncoding.GetByteCount(path) <= MaxNameBytes;
    }
}