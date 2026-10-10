using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using BH.SDK.Services.Content;
using ICSharpCode.SharpZipLib.Tar;

namespace BH.SDK.Services.Archive
{
    // POSIX tar in gzip: `tar -xzf`, Explorer, 7-Zip, Keka and Ark open it. No random access, hence
    // documents first and ReadLeadingAsync. Link entries are refused (no name check sees them).
    // The codec is synchronous: this library never picks the thread. Docs/ARCHIVE.md.

    /// <summary> Packing and unpacking the tar.gz a level archive is made of. </summary>
    public static class TarGzService
    {
        private const int CopyBufferSize = 81920;

        // GNU tar writes a leading "./" and ContentPath refuses a "." segment: stripping it is the one
        // normalization, a fact about tar rather than a repair.
        private const string CurrentDirectoryPrefix = "./";

        /// <summary> Writes entries into a tar.gz on the destination stream, in the order given -
        /// which is what lets a reader stop early, so the caller decides it. </summary>
        public static async Task PackAsync(IReadOnlyList<ArchiveEntrySource> entries, Stream destination,
            ArchivePolicy policy = null, CancellationToken token = default)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            policy = policy ?? ArchivePolicy.Default;

            if (policy.MaxNameBytes > ArchivePolicy.MaxEntryNameBytes)
                throw new ArgumentException(
                    $"A tar header holds {ArchivePolicy.MaxEntryNameBytes} bytes of name; a policy asking for " +
                    $"{policy.MaxNameBytes} is a zip-only policy.", nameof(policy));

            using (var gzip = new GZipStream(destination, policy.CompressionLevel, leaveOpen: true))
            using (var tar = new TarOutputStream(gzip, ArchivePolicy.NameEncoding) { IsStreamOwner = false })
            {
                foreach (var entry in entries)
                {
                    token.ThrowIfCancellationRequested();

                    if (!ArchivePolicy.FitsName(entry.Path))
                        throw new InvalidDataException(
                            $"'{entry.Path}' is longer than the {ArchivePolicy.MaxEntryNameBytes} bytes a tar " +
                            "header can hold. Rename it before packing.");

                    var length = await entry.GetLengthAsync(token);
                    await tar.PutNextEntryAsync(CreateEntry(entry.Path, length), token);

                    using (var content = await entry.OpenReadAsync(token))
                        await content.CopyToAsync(tar, CopyBufferSize, token);

                    await tar.CloseEntryAsync(token);
                }
            }
        }

        /// <summary> Reads a tar.gz into a store, refusing anything that fails one of the four
        /// checks. Returns what was written, in the order the archive held it. </summary>
        public static async Task<IReadOnlyList<string>> UnpackAsync(Stream source, IContentStore destination,
            ArchiveLimits limits = null, CancellationToken token = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            limits = limits ?? ArchiveLimits.Default;
            limits.Validate();

            var written = new List<string>();
            var entryCount = 0;
            var totalBytes = 0L;

            using (var gzip = new GZipStream(source, CompressionMode.Decompress, leaveOpen: true))
            using (var tar = new TarInputStream(gzip, ArchivePolicy.NameEncoding) { IsStreamOwner = false })
            {
                TarEntry entry;
                while ((entry = await tar.GetNextEntryAsync(token)) != null)
                {
                    token.ThrowIfCancellationRequested();

                    if (++entryCount > limits.MaxEntries)
                        throw new InvalidDataException(
                            $"Archive holds more than the {limits.MaxEntries} entries allowed.");

                    RequireCarryableType(entry);

                    // A directory entry carries no content and the store has no concept of one -
                    // whatever writes a blob under it creates what it needs. It is checked for its
                    // TYPE above and then skipped, which is not the same as ignoring it.
                    if (entry.IsDirectory) continue;

                    var name = Normalize(entry.Name);
                    if (!ContentPath.TryValidate(name, out var error))
                        throw new InvalidDataException($"Archive entry '{entry.Name}' is refused: {error}.");

                    if (entry.Size > limits.MaxEntryBytes)
                        throw new InvalidDataException(
                            $"Archive entry '{name}' declares {entry.Size} bytes, over the " +
                            $"{limits.MaxEntryBytes} byte limit.");

                    totalBytes += entry.Size;
                    if (totalBytes > limits.MaxTotalBytes)
                        throw new InvalidDataException(
                            $"Archive unpacks to more than the {limits.MaxTotalBytes} bytes allowed.");

                    using (var output = await destination.OpenWriteAsync(name, token))
                        await tar.CopyEntryContentsAsync(output, token);

                    written.Add(name);
                }
            }

            return written;
        }

        /// <summary> Reads only the named entries, stopping as soon as all of them are found - the
        /// answer to tar.gz having no directory to seek into. </summary>
        public static async Task<IReadOnlyDictionary<string, byte[]>> ReadLeadingAsync(Stream source,
            IReadOnlyCollection<string> wanted, ArchiveLimits limits = null, CancellationToken token = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (wanted == null) throw new ArgumentNullException(nameof(wanted));

            limits = limits ?? ArchiveLimits.Default;
            limits.Validate();

            var remaining = new HashSet<string>(wanted, StringComparer.Ordinal);
            var found = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            if (remaining.Count == 0) return found;

            var entryCount = 0;

            using (var gzip = new GZipStream(source, CompressionMode.Decompress, leaveOpen: true))
            using (var tar = new TarInputStream(gzip, ArchivePolicy.NameEncoding) { IsStreamOwner = false })
            {
                TarEntry entry;
                while (remaining.Count > 0 && (entry = await tar.GetNextEntryAsync(token)) != null)
                {
                    token.ThrowIfCancellationRequested();

                    if (++entryCount > limits.MaxEntries)
                        throw new InvalidDataException(
                            $"Archive holds more than the {limits.MaxEntries} entries allowed.");

                    RequireCarryableType(entry);
                    if (entry.IsDirectory) continue;

                    var name = Normalize(entry.Name);
                    if (!remaining.Contains(name)) continue;

                    if (entry.Size > limits.MaxEntryBytes)
                        throw new InvalidDataException(
                            $"Archive entry '{name}' declares {entry.Size} bytes, over the " +
                            $"{limits.MaxEntryBytes} byte limit.");

                    using (var buffer = new MemoryStream())
                    {
                        await tar.CopyEntryContentsAsync(buffer, token);
                        found[name] = buffer.ToArray();
                    }

                    remaining.Remove(name);
                }
            }

            return found;
        }

        private static TarEntry CreateEntry(string path, long length)
        {
            var entry = TarEntry.CreateTarEntry(path);

            entry.TarHeader.TypeFlag = TarHeader.LF_NORMAL;
            entry.TarHeader.Mode = ArchivePolicy.FileMode;
            entry.ModTime = ArchivePolicy.PinnedModTime;
            entry.Size = length;
            entry.SetIds(0, 0);
            entry.SetNames(string.Empty, string.Empty);

            return entry;
        }

        // The check ZIP never needed. LF_OLDNORM (pre-ustar plain file) stays accepted; symlinks, hard
        // links, devices, FIFOs and the GNU extensions are an attack or a shape this format lacks.
        private static void RequireCarryableType(TarEntry entry)
        {
            var typeFlag = entry.TarHeader.TypeFlag;
            if (typeFlag == TarHeader.LF_NORMAL || typeFlag == TarHeader.LF_OLDNORM ||
                typeFlag == TarHeader.LF_DIR) return;

            throw new InvalidDataException(
                $"Archive entry '{entry.Name}' is of type '{(char)typeFlag}', which a level archive " +
                "may not carry - only plain files and directories are read.");
        }

        private static string Normalize(string name)
        {
            if (name == null) return null;

            return name.StartsWith(CurrentDirectoryPrefix, StringComparison.Ordinal)
                ? name.Substring(CurrentDirectoryPrefix.Length)
                : name;
        }
    }
}
