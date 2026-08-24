using System.Collections.Generic;
using BH.SDK.Interop;
using BH.SDK.Models;

namespace BH.SDK.Services.Package
{
    // What an export WILL do, decided before a single byte is written. Separating the decision from
    // the writing is what makes the whole thing testable without a disk and reportable before the
    // author commits to it: the same plan drives a folder, an archive, and either of them encrypted.
    //
    // THE LEVEL AND META HERE ARE COPIES, ALWAYS. An export collects a resource that lives outside
    // the level folder and rewrites its key to point inside the package instead - and the author
    // asked for an export, not for an edit of the level they are working on. Rewriting the live
    // model would silently change the open document.

    /// <summary> One file the package will carry, and where its bytes come from. </summary>
    public readonly struct PackageFile
    {
        /// <summary> Name inside the package. </summary>
        public readonly string PackagePath;

        /// <summary> Where to read it from - a path inside the level's own store, or an absolute
        /// path on this machine when the resource lived outside the level folder. </summary>
        public readonly string SourcePath;

        /// <summary> True when SourcePath is an absolute path rather than a store path. </summary>
        public readonly bool IsExternal;

        public PackageFile(string packagePath, string sourcePath, bool isExternal)
        {
            PackagePath = packagePath;
            SourcePath = sourcePath;
            IsExternal = isExternal;
        }

        public override string ToString() => IsExternal
            ? $"{PackagePath} <- {SourcePath} (collected)"
            : PackagePath;
    }

    /// <summary> Everything an export decided, before it writes anything. </summary>
    public sealed class LevelPackagePlan
    {
        public LevelPackagePlan(Level level, LevelMeta meta, IReadOnlyList<PackageFile> files,
            InteropReport report, int droppedFileCount)
        {
            Level = level;
            Meta = meta;
            Files = files;
            Report = report;
            DroppedFileCount = droppedFileCount;
        }

        /// <summary> The level as the package will carry it - a copy, with collected resources
        /// repointed at the package. </summary>
        public Level Level { get; }

        /// <summary> The metadata as the package will carry it - a copy, same treatment. </summary>
        public LevelMeta Meta { get; }

        /// <summary> The media the package will carry, in the order it will be written. </summary>
        public IReadOnlyList<PackageFile> Files { get; }

        /// <summary> What could not travel, what was collected, what was renamed. </summary>
        public InteropReport Report { get; }

        // A number rather than a list, and the number is the point: silently including files nobody
        // references and silently dropping them are equally wrong, and a count is what turns it into
        // something the author can see and decide about.

        /// <summary> How many files in the level folder nothing referenced, and were left out. </summary>
        public int DroppedFileCount { get; }
    }
}
