using System.Collections.Generic;
using BH.SDK.Interop;
using BH.SDK.Models;

namespace BH.SDK.Services.LevelArchive
{
    // What an export WILL do, decided before a byte is written. The level and meta are COPIES, always:
    // collecting rewrites keys, and rewriting the live model would change the open document.

    /// <summary> One file the archive will carry, and where its bytes come from. </summary>
    public readonly struct ArchiveFile
    {
        /// <summary> Name inside the archive. </summary>
        public readonly string ArchivePath;

        /// <summary> Where to read it from - a path inside the level's own store, or an absolute
        /// path on this machine when the resource lived outside the level folder. </summary>
        public readonly string SourcePath;

        /// <summary> True when SourcePath is an absolute path rather than a store path. </summary>
        public readonly bool IsExternal;

        /// <summary> Built from its path, path and external. </summary>
        public ArchiveFile(string archivePath, string sourcePath, bool isExternal)
        {
            ArchivePath = archivePath;
            SourcePath = sourcePath;
            IsExternal = isExternal;
        }

        /// <summary> One line, for a log. </summary>
        public override string ToString() => IsExternal
            ? $"{ArchivePath} <- {SourcePath} (collected)"
            : ArchivePath;
    }

    /// <summary> Everything an export decided, before it writes anything. </summary>
    public sealed class LevelArchivePlan
    {
        /// <summary> Every member at once, in declaration order. </summary>
        public LevelArchivePlan(Level level, LevelMeta meta, IReadOnlyList<ArchiveFile> files,
            InteropReport report, int droppedFileCount)
        {
            Level = level;
            Meta = meta;
            Files = files;
            Report = report;
            DroppedFileCount = droppedFileCount;
        }

        /// <summary> The level as the archive will carry it - a copy, with collected resources
        /// repointed at the archive. </summary>
        public Level Level { get; }

        /// <summary> The metadata as the archive will carry it - a copy, same treatment. </summary>
        public LevelMeta Meta { get; }

        /// <summary> The media the archive will carry, in the order it will be written. </summary>
        public IReadOnlyList<ArchiveFile> Files { get; }

        /// <summary> What could not travel, what was collected, what was renamed. </summary>
        public InteropReport Report { get; }

        // A number rather than a list, and the number is the point: silently including files nobody
        // references and silently dropping them are equally wrong, and a count is what turns it into
        // something the author can see and decide about.

        /// <summary> How many files in the level folder nothing referenced, and were left out. </summary>
        public int DroppedFileCount { get; }
    }
}
