using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BH.SDK.Models;
using BH.SDK.Models.Profile;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Services.Archive;
using BH.SDK.Services.Content;
using BH.SDK.Versions;

namespace BH.SDK.Services.Profile
{
    /// <summary> How peeking at a profile archive ended. </summary>
    public enum ProfileReadResult
    {
        /// <summary> A profile this build can read. </summary>
        Ok = 0,

        /// <summary> Not a zip - a tar.gz, a 7z, an OpenPGP message, or not an archive at all. </summary>
        NotAZip = 1,

        /// <summary> A zip, but with no profile manifest at its root. </summary>
        NotAProfile = 2,

        /// <summary> A zip with encrypted entries; a profile is never written that way. </summary>
        Encrypted = 3,

        /// <summary> Written by a newer build. The manifest, when readable, says which. </summary>
        NewerGeneration = 4,

        /// <summary> The directory or the manifest cannot be read. </summary>
        Damaged = 5,
    }

    /// <summary> What a profile archive's directory and manifest say, read without unpacking. </summary>
    public sealed class ProfilePeek
    {
        internal ProfilePeek(ProfileReadResult result, ProfileManifest manifest,
            IReadOnlyList<ArchiveEntryInfo> entries)
        {
            Result = result;
            Manifest = manifest;
            Entries = entries ?? Array.Empty<ArchiveEntryInfo>();

            var files = new Dictionary<ProfileCategory, int>();
            var bytes = new Dictionary<ProfileCategory, long>();

            foreach (var entry in Entries)
            {
                var category = ProfileLayout.CategoryOf(entry.Name);
                if (category == ProfileCategory.None) continue;

                files[category] = (files.TryGetValue(category, out var count) ? count : 0) + 1;
                bytes[category] = (bytes.TryGetValue(category, out var size) ? size : 0L) + entry.DeclaredBytes;
                if (entry.DeclaredBytes > DeclaredMaxEntryBytes) DeclaredMaxEntryBytes = entry.DeclaredBytes;
            }

            FilesByCategory = files;
            BytesByCategory = bytes;
        }

        /// <summary> The answer. Anything but Ok leaves the archive unusable. </summary>
        public ProfileReadResult Result { get; }

        /// <summary> The manifest, when it could be read - also for a newer generation. </summary>
        public ProfileManifest Manifest { get; }

        /// <summary> Every file entry the directory lists. </summary>
        public IReadOnlyList<ArchiveEntryInfo> Entries { get; }

        /// <summary> Profile files per category. A category with none is absent. </summary>
        public IReadOnlyDictionary<ProfileCategory, int> FilesByCategory { get; }

        /// <summary> Declared uncompressed bytes per category. </summary>
        public IReadOnlyDictionary<ProfileCategory, long> BytesByCategory { get; }

        /// <summary> The largest single entry the directory declares. </summary>
        public long DeclaredMaxEntryBytes { get; }

        /// <summary> The categories the archive carries - the manifest's own list. </summary>
        public ProfileCategory Categories => Manifest?.Categories ?? ProfileCategory.None;

        /// <summary> Declared uncompressed bytes of the given categories. </summary>
        public long DeclaredBytesOf(ProfileCategory categories)
        {
            var total = 0L;
            foreach (var pair in BytesByCategory)
                if ((categories & pair.Key) != 0)
                    total += pair.Value;
            return total;
        }

        /// <summary> Files of the given categories. </summary>
        public int FilesOf(ProfileCategory categories)
        {
            var total = 0;
            foreach (var pair in FilesByCategory)
                if ((categories & pair.Key) != 0)
                    total += pair.Value;
            return total;
        }
    }

    // EVERY REFUSAL IS A VALUE, LevelArchiveReader's contract: the host turns each into a sentence for
    // the player, and a future server turns each into a status code - neither wants to catch.
    //
    // THE UNPACK LIMITS ARE WHAT THE DIRECTORY DECLARED. Before anything is written the player has been
    // shown the size and the disk has been checked against it, so a file that inflates past its own
    // claim is lying, and that is exactly what the copy's hard counter stops. A bomb can make the
    // directory claim a lot - which the player sees - but cannot make the disk receive more than that.

    /// <summary> Reads a profile archive: its manifest and directory first, then the chosen categories. </summary>
    public static class ProfileArchiveReader
    {
        /// <summary> Reads the directory and the manifest. The stream must be seekable. </summary>
        public static async Task<ProfilePeek> PeekAsync(Stream source, SerializationService serialization,
            CancellationToken token = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (serialization == null) throw new ArgumentNullException(nameof(serialization));
            if (!source.CanSeek)
                throw new ArgumentException("A profile is sniffed and then re-read, so its stream must be seekable.",
                    nameof(source));

            var origin = source.Position;
            var leading = new byte[ArchiveFormatSniffer.MagicBytes];
            var read = await source.ReadAsync(leading, 0, leading.Length, token);
            if (read < leading.Length) Array.Resize(ref leading, read);
            source.Position = origin;

            if (ArchiveFormatSniffer.Detect(leading) != ArchiveFormat.Zip)
                return new ProfilePeek(ProfileReadResult.NotAZip, null, null);

            IReadOnlyList<ArchiveEntryInfo> entries;
            try
            {
                entries = ZipService.ListEntries(source, ProfileArchive.DirectoryLimits);
            }
            catch (InvalidDataException)
            {
                return new ProfilePeek(ProfileReadResult.Damaged, null, null);
            }
            finally
            {
                source.Position = origin;
            }

            var hasManifest = false;
            foreach (var entry in entries)
            {
                if (entry.Encrypted) return new ProfilePeek(ProfileReadResult.Encrypted, null, entries);
                if (ProfileLayout.IsManifest(entry.Name)) hasManifest = true;
            }

            if (!hasManifest) return new ProfilePeek(ProfileReadResult.NotAProfile, null, entries);

            ProfileManifest manifest;
            try
            {
                var found = await ZipService.ReadEntriesAsync(source, new[] { FileNames.ProfileManifestFileName },
                    ProfileArchive.DirectoryLimits, token: token);
                manifest = serialization.DeserializeEnvelope<ProfileManifest>(
                    found[FileNames.ProfileManifestFileName], SerializationType.Json);
            }
            catch (NewerGenerationException)
            {
                return new ProfilePeek(ProfileReadResult.NewerGeneration, null, entries);
            }
            catch (Exception error) when (!(error is OperationCanceledException))
            {
                return new ProfilePeek(ProfileReadResult.Damaged, null, entries);
            }
            finally
            {
                source.Position = origin;
            }

            if (manifest == null) return new ProfilePeek(ProfileReadResult.Damaged, null, entries);

            return manifest.ModelGeneration > ModelGenerations.Current
                ? new ProfilePeek(ProfileReadResult.NewerGeneration, manifest, entries)
                : new ProfilePeek(ProfileReadResult.Ok, manifest, entries);
        }

        /// <summary> Unpacks the <paramref name="selected"/> categories into <paramref name="into"/>,
        /// under their archive paths. Returns what was written. Throws <see cref="InvalidDataException"/>
        /// when an entry fails the unpack checks or inflates past what the directory declared. </summary>
        public static Task<IReadOnlyList<string>> UnpackAsync(Stream source, IContentStore into,
            ProfileCategory selected, ProfilePeek peek, CancellationToken token = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (into == null) throw new ArgumentNullException(nameof(into));
            if (peek == null) throw new ArgumentNullException(nameof(peek));
            if (peek.Result != ProfileReadResult.Ok)
                throw new InvalidOperationException($"A profile that peeked as {peek.Result} is not unpacked.");

            var limits = new ArchiveLimits
            {
                MaxEntries = ProfileArchive.MaxEntries,
                MaxEntryBytes = Math.Max(1L, Math.Min(peek.DeclaredMaxEntryBytes, ProfileArchive.MaxTotalBytes)),
                MaxTotalBytes = Math.Max(1L, Math.Min(peek.DeclaredBytesOf(selected), ProfileArchive.MaxTotalBytes)),
            };

            return ZipService.UnpackAsync(source, into,
                path => !ProfileLayout.IsManifest(path) && (ProfileLayout.CategoryOf(path) & selected) != 0,
                limits, token: token);
        }
    }
}
