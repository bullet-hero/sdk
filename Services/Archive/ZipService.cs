using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BH.SDK.Services.Content;
using ICSharpCode.SharpZipLib.Zip;

namespace BH.SDK.Services.Archive
{
    // The container Explorer opens on a double click, with a central directory (ReadEntriesAsync).
    // The four unpack checks are tar's, two harder: a declared size is a CLAIM (enforced twice), and
    // duplicate names are refused. Two readers on purpose; ZipCrypto read, never written. Docs/ARCHIVE.md.

    /// <summary> Packing and unpacking the zip a level archive can be made of. </summary>
    public static class ZipService
    {
        private const int CopyBufferSize = 81920;
        private const string CurrentDirectoryPrefix = "./";

        // The unix mode bits a plain file carries, in the high half of ExternalFileAttributes:
        // S_IFREG plus the policy's own permission bits. The mask is what a READ looks at.
        private const int UnixFileTypeMask = 0xF000;
        private const int UnixRegularFile = 0x8000;
        private const int UnixDirectory = 0x4000;

        // A zip written on a machine that has no unix modes leaves the high half at zero, and that
        // is not a suspicious entry - it is every archive Explorer has ever made.
        private const int NoUnixMode = 0;

        // The passphrase is UTF-8: SharpZipLib defaults to the ANSI code page, 7-Zip and every AE-2
        // implementation use UTF-8, so a Cyrillic password only opens with this codec (measured with
        // 7-Zip). Only the crypto encoding is overridden; entry names keep their own (Docs/ARCHIVE.md).
        private static readonly StringCodec PassphraseCodec =
            StringCodec.Default.WithZipCryptoEncoding(new UTF8Encoding(false));

        /// <summary> Writes entries into a zip on the destination stream, in the order given. </summary>
        public static Task PackAsync(IReadOnlyList<ArchiveEntrySource> entries, Stream destination,
            ArchivePolicy policy = null, CancellationToken token = default) =>
            PackCoreAsync(entries, destination, policy, passphrase: null, token);

        /// <summary> Writes the same zip with every entry encrypted, WinZip AE-2 with AES-256 - the
        /// shape 7-Zip opens by asking for a password. </summary>
        public static Task PackEncryptedAsync(IReadOnlyList<ArchiveEntrySource> entries, Stream destination,
            char[] passphrase, ArchivePolicy policy = null, CancellationToken token = default)
        {
            if (passphrase == null || passphrase.Length == 0)
                throw new ArgumentException("An encrypted zip needs a passphrase.", nameof(passphrase));

            return PackCoreAsync(entries, destination, policy, passphrase, token);
        }

        private static async Task PackCoreAsync(IReadOnlyList<ArchiveEntrySource> entries, Stream destination,
            ArchivePolicy policy, char[] passphrase, CancellationToken token)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            policy = policy ?? ArchivePolicy.Default;

            // The wrapper is what keeps one input producing one output: given a seekable
            // destination ZipOutputStream patches the local header afterwards, given a pipe it
            // writes a data descriptor, and those are different bytes for the same archive.
            using (var pipe = new NonSeekableWrite(destination))
            using (var zip = new ZipOutputStream(pipe, PassphraseCodec)
                   { IsStreamOwner = false, UseZip64 = UseZip64.Dynamic })
            {
                zip.SetLevel(DeflateLevelOf(policy.CompressionLevel));

                // SharpZipLib takes a string, which cannot be zeroed like the char[] elsewhere: built
                // as late as possible, dropped as soon as the archive is finished. A mitigation only.
                if (passphrase != null) zip.Password = new string(passphrase);

                try
                {
                    foreach (var entry in entries)
                    {
                        token.ThrowIfCancellationRequested();

                        if (!policy.Fits(entry.Path))
                            throw new InvalidDataException(
                                $"'{entry.Path}' is longer than the {policy.MaxNameBytes} bytes " +
                                "an entry name may take. Rename it before packing.");

                        var length = await entry.GetLengthAsync(token);
                        await zip.PutNextEntryAsync(CreateEntry(entry.Path, length, passphrase != null), token);

                        using (var content = await entry.OpenReadAsync(token))
                            await content.CopyToAsync(zip, CopyBufferSize, token);

                        await zip.CloseEntryAsync(token);
                    }

                    await zip.FinishAsync(token);
                }
                finally
                {
                    zip.Password = null;
                }
            }
        }

        /// <summary> Reads a zip into a store, refusing anything that fails one of the four checks.
        /// Returns what was written, in the order the archive held it. </summary>
        public static Task<IReadOnlyList<string>> UnpackAsync(Stream source, IContentStore destination,
            ArchiveLimits limits = null, char[] passphrase = null, CancellationToken token = default) =>
            UnpackAsync(source, destination, include: null, limits, passphrase, token);

        // An entry left out is still JUDGED (name, type, duplicates); only its inflating is skipped.
        // The size limits count the KEPT entries only: those are the bytes that reach the store.

        /// <summary> Reads only the entries <paramref name="include"/> accepts into a store; every
        /// entry is still checked. A null filter keeps everything. </summary>
        public static async Task<IReadOnlyList<string>> UnpackAsync(Stream source, IContentStore destination,
            Func<string, bool> include, ArchiveLimits limits = null, char[] passphrase = null,
            CancellationToken token = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            limits = limits ?? ArchiveLimits.Default;
            limits.Validate();

            var written = new List<string>();

            using (var zip = Open(source, passphrase))
            {
                RequireDeclaredSizeWithinLimits(zip, limits, include);

                var taken = new HashSet<string>(StringComparer.Ordinal);
                var totalBytes = 0L;

                foreach (ZipEntry entry in zip)
                {
                    token.ThrowIfCancellationRequested();

                    RequireCarryableType(entry);
                    if (entry.IsDirectory) continue;

                    var name = RequireName(entry, taken);
                    if (include != null && !include(name)) continue;

                    RequireOpenable(entry, passphrase);

                    if (entry.Size > limits.MaxEntryBytes)
                        throw new InvalidDataException(
                            $"Archive entry '{name}' declares {entry.Size} bytes, over the " +
                            $"{limits.MaxEntryBytes} byte limit.");

                    using (var content = OpenEntry(zip, entry))
                    using (var output = await destination.OpenWriteAsync(name, token))
                        totalBytes = await CopyGuardedAsync(content, output, name, totalBytes, limits, token);

                    written.Add(name);
                }
            }

            return written;
        }

        /// <summary> Reads only the named entries, out of the central directory rather than by
        /// reading up to them - the thing tar.gz cannot do. </summary>
        public static async Task<IReadOnlyDictionary<string, byte[]>> ReadEntriesAsync(Stream source,
            IReadOnlyCollection<string> wanted, ArchiveLimits limits = null, char[] passphrase = null,
            CancellationToken token = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (wanted == null) throw new ArgumentNullException(nameof(wanted));

            limits = limits ?? ArchiveLimits.Default;
            limits.Validate();

            var found = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            if (wanted.Count == 0) return found;

            using (var zip = Open(source, passphrase))
            {
                RequireDeclaredSizeWithinLimits(zip, limits);

                var totalBytes = 0L;

                foreach (var name in wanted)
                {
                    token.ThrowIfCancellationRequested();

                    var index = zip.FindEntry(name, ignoreCase: false);
                    if (index < 0) continue;

                    var entry = zip[(int)index];
                    RequireCarryableType(entry);
                    if (entry.IsDirectory) continue;

                    RequireOpenable(entry, passphrase);

                    if (entry.Size > limits.MaxEntryBytes)
                        throw new InvalidDataException(
                            $"Archive entry '{name}' declares {entry.Size} bytes, over the " +
                            $"{limits.MaxEntryBytes} byte limit.");

                    using (var content = OpenEntry(zip, entry))
                    using (var buffer = new MemoryStream())
                    {
                        totalBytes = await CopyGuardedAsync(content, buffer, name, totalBytes, limits, token);
                        found[name] = buffer.ToArray();
                    }
                }
            }

            return found;
        }

        /// <summary> The entry names a zip holds, and whether opening it needs a passphrase - read
        /// from the directory, without unpacking anything. </summary>
        public static IReadOnlyList<string> List(Stream source, out bool encrypted,
            ArchiveLimits limits = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            limits = limits ?? ArchiveLimits.Default;
            limits.Validate();

            var names = new List<string>();
            encrypted = false;

            using (var zip = Open(source, passphrase: null))
            {
                RequireDeclaredSizeWithinLimits(zip, limits);

                foreach (ZipEntry entry in zip)
                {
                    if (entry.IsCrypted) encrypted = true;
                    if (entry.IsDirectory) continue;

                    names.Add(Normalize(entry.Name));
                }
            }

            return names;
        }

        /// <summary> Every file entry with what its directory claims about it - name, uncompressed
        /// size, whether it is encrypted - without inflating anything. </summary>
        public static IReadOnlyList<ArchiveEntryInfo> ListEntries(Stream source, ArchiveLimits limits = null)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            limits = limits ?? ArchiveLimits.Default;
            limits.Validate();

            var entries = new List<ArchiveEntryInfo>();

            using (var zip = Open(source, passphrase: null))
            {
                RequireDeclaredSizeWithinLimits(zip, limits);

                foreach (ZipEntry entry in zip)
                {
                    if (entry.IsDirectory) continue;
                    entries.Add(new ArchiveEntryInfo(Normalize(entry.Name), Math.Max(0L, entry.Size),
                        entry.IsCrypted));
                }
            }

            return entries;
        }

        private static ZipFile Open(Stream source, char[] passphrase)
        {
            ZipFile zip;

            // The directory is read by the CONSTRUCTOR, so a truncated or hostile file throws here
            // rather than at the first entry. Translated on the spot, since from the outside a zip
            // whose directory does not parse is simply damaged.
            try
            {
                zip = new ZipFile(source, leaveOpen: true, stringCodec: PassphraseCodec)
                {
                    IsStreamOwner = false,
                };
            }
            catch (Exception error) when (error is ZipException || error is EndOfStreamException ||
                                          error is IndexOutOfRangeException)
            {
                throw new InvalidDataException("This zip's directory cannot be read: " + error.Message, error);
            }

            // Left at false deliberately: it makes ZipFile compare each local header with the
            // central one it was found through, which is a consistency check tar has no equivalent
            // of and costs one seek per entry.
            zip.SkipLocalEntryTestsOnLocate = false;

            if (passphrase != null && passphrase.Length > 0) zip.Password = new string(passphrase);
            return zip;
        }

        private static void RequireDeclaredSizeWithinLimits(ZipFile zip, ArchiveLimits limits,
            Func<string, bool> include = null)
        {
            if (zip.Count > limits.MaxEntries)
                throw new InvalidDataException(
                    $"Archive holds more than the {limits.MaxEntries} entries allowed.");

            var declared = 0L;
            foreach (ZipEntry entry in zip)
            {
                if (entry.Size <= 0) continue;
                if (include != null && !include(Normalize(entry.Name))) continue;

                declared += entry.Size;
                if (declared > limits.MaxTotalBytes)
                    throw new InvalidDataException(
                        $"Archive declares more than the {limits.MaxTotalBytes} bytes allowed.");
            }
        }

        private static Stream OpenEntry(ZipFile zip, ZipEntry entry)
        {
            try
            {
                return zip.GetInputStream(entry);
            }
            catch (ZipException error)
            {
                throw Translate(error, entry.Name);
            }
        }

        // The declared size is a claim; this is what makes it one that cannot hurt. A bomb passes
        // every directory check by lying in it, so the copy counts what actually arrives and stops
        // the moment either cap is passed - the entry's own, or the archive's total.
        private static async Task<long> CopyGuardedAsync(Stream content, Stream output, string name,
            long totalBytes, ArchiveLimits limits, CancellationToken token)
        {
            var buffer = new byte[CopyBufferSize];
            var entryBytes = 0L;

            while (true)
            {
                int read;
                try
                {
                    read = await content.ReadAsync(buffer, 0, buffer.Length, token);
                }
                catch (ZipException error)
                {
                    throw Translate(error, name);
                }

                if (read <= 0) break;

                entryBytes += read;
                totalBytes += read;

                if (entryBytes > limits.MaxEntryBytes)
                    throw new InvalidDataException(
                        $"Archive entry '{name}' unpacks to more than the {limits.MaxEntryBytes} bytes allowed.");

                if (totalBytes > limits.MaxTotalBytes)
                    throw new InvalidDataException(
                        $"Archive unpacks to more than the {limits.MaxTotalBytes} bytes allowed.");

                await output.WriteAsync(buffer, 0, read, token);
            }

            return totalBytes;
        }

        private static ZipEntry CreateEntry(string path, long length, bool encrypted)
        {
            var entry = new ZipEntry(path)
            {
                DateTime = ArchivePolicy.PinnedModTime,
                Size = length,
                CompressionMethod = CompressionMethod.Deflated,
                HostSystem = (int)HostSystemID.Unix,
                ExternalFileAttributes = (UnixRegularFile | ArchivePolicy.FileMode) << 16,
                IsUnicodeText = true,
            };

            // AE-2, and only 256: the setter refuses anything but 0, 128 and 256, and a weaker key
            // would be a choice nobody asked for. The archive stops being byte-reproducible here,
            // since the salt is random - the same is true of a .gpg and for the same reason.
            if (encrypted) entry.AESKeySize = 256;

            return entry;
        }

        // A zip entry's kind lives in the unix mode of ExternalFileAttributes; zero there (every Explorer
        // archive) means none recorded and must be accepted.
        private static void RequireCarryableType(ZipEntry entry)
        {
            if (entry.CompressionMethod != CompressionMethod.Stored &&
                entry.CompressionMethod != CompressionMethod.Deflated)
                throw new InvalidDataException(
                    $"Archive entry '{entry.Name}' is compressed with {entry.CompressionMethod}, which a " +
                    "level archive does not carry - only stored and deflated entries are read.");

            var mode = (entry.ExternalFileAttributes >> 16) & UnixFileTypeMask;
            if (mode == NoUnixMode || mode == UnixRegularFile) return;
            if (mode == UnixDirectory && entry.IsDirectory) return;

            throw new InvalidDataException(
                $"Archive entry '{entry.Name}' is of unix type 0x{mode:X4}, which a level archive may not " +
                "carry - only plain files and directories are read.");
        }

        private static string RequireName(ZipEntry entry, HashSet<string> taken)
        {
            var raw = entry.Name;

            // Some Windows writers store a backslash as the separator. ContentPath refuses rather
            // than repairs, and so does this: a name nobody agrees on is a name two tools disagree
            // about where to put.
            if (raw != null && raw.IndexOf('\\') >= 0)
                throw new InvalidDataException(
                    $"Archive entry '{raw}' is refused: a backslash is not a path separator here.");

            var name = Normalize(raw);
            if (!ContentPath.TryValidate(name, out var error))
                throw new InvalidDataException($"Archive entry '{raw}' is refused: {error}.");

            // Two entries under one name is the zip-specific trap: whatever inspected the archive
            // saw the first, whatever extracted it wrote the second.
            if (!taken.Add(name))
                throw new InvalidDataException(
                    $"Archive entry '{name}' appears twice, and which one it holds cannot be answered.");

            return name;
        }

        private static void RequireOpenable(ZipEntry entry, char[] passphrase)
        {
            if (entry.IsCrypted && (passphrase == null || passphrase.Length == 0))
                throw new ArchivePassphraseRequiredException(
                    $"Archive entry '{entry.Name}' is encrypted and no passphrase was given.");
        }

        private static Exception Translate(ZipException error, string name)
        {
            var message = error.Message ?? string.Empty;

            // Three answers rather than two, the same split PgpSymmetricService makes with the MDC:
            // "no passphrase yet" is a question to ask, "wrong passphrase" is an answer already
            // given, and a failed authentication code is a damaged file rather than either.
            if (message.IndexOf("No password", StringComparison.OrdinalIgnoreCase) >= 0)
                return new ArchivePassphraseRequiredException(
                    $"Archive entry '{name}' is encrypted and no passphrase was given.", error);

            if (message.IndexOf("Invalid password", StringComparison.OrdinalIgnoreCase) >= 0)
                return new ArchiveWrongPassphraseException(
                    $"The passphrase does not open archive entry '{name}'.", error);

            return new InvalidDataException($"Archive entry '{name}' cannot be read: {message}", error);
        }

        private static string Normalize(string name)
        {
            if (name == null) return null;

            return name.StartsWith(CurrentDirectoryPrefix, StringComparison.Ordinal)
                ? name.Substring(CurrentDirectoryPrefix.Length)
                : name;
        }

        // Deflate's own 0..9, spelled out rather than handed to the BCL to interpret: the number
        // has to be the same number on every runtime for two packs to be the same bytes.
        private static int DeflateLevelOf(System.IO.Compression.CompressionLevel level)
        {
            switch (level)
            {
                case System.IO.Compression.CompressionLevel.NoCompression: return 0;
                case System.IO.Compression.CompressionLevel.Fastest: return 1;
                default: return 9;
            }
        }
    }
}
