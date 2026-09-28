using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Services.Archive;
using BH.SDK.Services.Content;

namespace BH.SDK.Services.Collections
{
    /// <summary> How opening a collection archive ended. </summary>
    public enum CollectionArchiveResult
    {
        /// <summary> Unpacked and read. </summary>
        Ok = 0,

        /// <summary> Not a tar.gz or a zip at all. </summary>
        NotAnArchive = 1,

        /// <summary> A container this build cannot open (7z) or one that is encrypted. </summary>
        Unsupported = 2,

        /// <summary> Refused by the unpack checks, or over a limit. </summary>
        Damaged = 3,

        /// <summary> An archive, but with no collection manifest at its root. </summary>
        NotACollection = 4,

        /// <summary> The manifest was written by a newer build. </summary>
        NewerGeneration = 5,
    }

    // THE SAME TWO CONTAINERS A LEVEL USES, AND NOTHING ELSE. A collection is a folder, so its archive is
    // that folder packed by TarGzService or ZipService - which a player can open with any archiver, and
    // which is what Android and iOS move collections with, having no Workshop. The level archive's rules
    // hold unchanged: the format is sniffed from the bytes, the unpack is bounded by ArchiveLimits into
    // a store that cannot address outside itself, and every refusal is a value. Encryption is not
    // offered: a collection exists to be shared.
    //
    // THE MANIFEST IS PACKED FIRST, for the reason the level archive packs its metadata first - a tar
    // reader can stop early - even though nothing peeks a collection archive yet.

    /// <summary> Packs a collection folder into an archive, and reads one back. </summary>
    public static class CollectionArchive
    {
        /// <summary> Packs every file of the store, manifest first. </summary>
        public static async Task PackAsync(IContentStore collection, Stream destination, ArchiveFormat format,
            CancellationToken token = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (format != ArchiveFormat.TarGz && format != ArchiveFormat.Zip)
                throw new ArgumentException("A collection packs as a tar.gz or a zip.", nameof(format));

            var listing = await collection.ListAsync(string.Empty, token);
            var entries = new List<ArchiveEntrySource>(listing.Count);

            foreach (var path in listing)
                if (IsManifest(path))
                    entries.Add(ArchiveEntrySource.FromStore(path, collection));
            foreach (var path in listing)
                if (!IsManifest(path))
                    entries.Add(ArchiveEntrySource.FromStore(path, collection));

            if (format == ArchiveFormat.Zip)
                await ZipService.PackAsync(entries, destination, token: token);
            else
                await TarGzService.PackAsync(entries, destination, token: token);
        }

        /// <summary> Unpacks an archive into <paramref name="into"/> and reads it as a collection. The
        /// stream must be seekable - it is sniffed, then read from the start. </summary>
        public static async Task<(CollectionArchiveResult Result, CollectionContent Content)> UnpackAsync(
            Stream source, IContentStore into, SerializationService serialization, ArchiveLimits limits = null,
            CancellationToken token = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (into == null) throw new ArgumentNullException(nameof(into));
            if (!source.CanSeek)
                throw new ArgumentException("An archive is sniffed and then re-read, so its stream must be seekable.",
                    nameof(source));

            limits = limits ?? ArchiveLimits.Default;

            var origin = source.Position;
            var leading = new byte[ArchiveFormatSniffer.MagicBytes];
            var read = await source.ReadAsync(leading, 0, leading.Length, token);
            if (read < leading.Length) Array.Resize(ref leading, read);
            source.Position = origin;

            var format = ArchiveFormatSniffer.Detect(leading);
            if (format == ArchiveFormat.SevenZip || format == ArchiveFormat.OpenPgp)
                return (CollectionArchiveResult.Unsupported, null);
            if (format != ArchiveFormat.TarGz && format != ArchiveFormat.Zip)
                return (CollectionArchiveResult.NotAnArchive, null);

            try
            {
                if (format == ArchiveFormat.Zip)
                    await ZipService.UnpackAsync(source, into, limits, null, token);
                else
                    await TarGzService.UnpackAsync(source, into, limits, token);
            }
            catch (InvalidDataException)
            {
                return (CollectionArchiveResult.Damaged, null);
            }
            catch (ArchivePassphraseRequiredException)
            {
                return (CollectionArchiveResult.Unsupported, null);
            }

            var outcome = await CollectionReader.ReadAsync(into, serialization, token);
            return outcome.Result switch
            {
                CollectionReadResult.Ok => (CollectionArchiveResult.Ok, outcome.Content),
                CollectionReadResult.NewerGeneration => (CollectionArchiveResult.NewerGeneration, null),
                CollectionReadResult.NotACollection => (CollectionArchiveResult.NotACollection, null),
                _ => (CollectionArchiveResult.Damaged, null),
            };
        }

        /// <summary> Copies every file of one store into another - how an archive unpacked into memory
        /// lands in its final folder once its id is known. </summary>
        public static async Task CopyAsync(IContentStore from, IContentStore to, CancellationToken token = default)
        {
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));

            foreach (var path in await from.ListAsync(string.Empty, token))
            {
                using var source = await from.OpenReadAsync(path, token);
                using var destination = await to.OpenWriteAsync(path, token);
                await source.CopyToAsync(destination, 81920, token);
            }
        }

        private static bool IsManifest(string path)
        {
            foreach (var format in SerializationTypeExtensions.ProbeOrder)
                if (path == CollectionReader.ManifestPath(format))
                    return true;
            return false;
        }
    }
}
