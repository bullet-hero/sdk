using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BH.SDK.Models;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Services.Archive;
using BH.SDK.Services.Content;
using BH.SDK.Services.Crypto;

namespace BH.SDK.Services.Collections
{
    /// <summary> How opening a collection archive ended. </summary>
    public enum CollectionArchiveResult
    {
        /// <summary> Unpacked and read. </summary>
        Ok = 0,

        /// <summary> Not a tar.gz or a zip at all. </summary>
        NotAnArchive = 1,

        /// <summary> A container this build cannot open (7z). </summary>
        Unsupported = 2,

        /// <summary> Refused by the unpack checks, or over a limit. </summary>
        Damaged = 3,

        /// <summary> An archive, but with no collection manifest at its root. </summary>
        NotACollection = 4,

        /// <summary> The manifest was written by a newer build. </summary>
        NewerGeneration = 5,

        /// <summary> Encrypted, and no passphrase was given. Asking for one and trying again is the answer. </summary>
        PassphraseRequired = 6,

        /// <summary> Encrypted, and the passphrase given does not open it. </summary>
        WrongPassphrase = 7,
    }

    // THE SAME TWO CONTAINERS A LEVEL USES, AND NOTHING ELSE. A collection is a folder, so its archive is
    // that folder packed by TarGzService or ZipService - which a player can open with any archiver, and
    // which is what Android and iOS move collections with, having no Workshop. The level archive's rules
    // hold unchanged: the format is sniffed from the bytes, the unpack is bounded by ArchiveLimits into
    // a store that cannot address outside itself, and every refusal is a value.
    //
    // ENCRYPTION IS THE LEVEL ARCHIVE'S TWO SCHEMES, for any content: the whole archive wrapped in an
    // OpenPGP symmetric message (a .tar.gz.gpg or a .zip.gpg, which gpg opens), or a zip whose entries are
    // AES-256 (which 7-Zip opens). What comes out of an OpenPGP message is sniffed again, since only the
    // plaintext says which container it holds. A missing or wrong passphrase is a value the host answers
    // by asking, never an exception.
    //
    // THE MANIFEST IS PACKED FIRST, for the reason the level archive packs its metadata first - a tar
    // reader can stop early - even though nothing peeks a collection archive yet.

    /// <summary> Packs a collection folder into an archive, and reads one back. </summary>
    public static class CollectionArchive
    {
        /// <summary> Packs every file of the store, manifest first. </summary>
        public static Task PackAsync(IContentStore collection, Stream destination, ArchiveFormat format,
            CancellationToken token = default) =>
            PackAsync(collection, destination, format, ArchiveProtection.None, null, token);

        /// <summary> Packs every file of the store, manifest first, protected by
        /// <paramref name="protection"/>. AES exists only inside a zip. </summary>
        public static async Task PackAsync(IContentStore collection, Stream destination, ArchiveFormat format,
            ArchiveProtection protection, char[] passphrase, CancellationToken token = default)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (format != ArchiveFormat.TarGz && format != ArchiveFormat.Zip)
                throw new ArgumentException("A collection packs as a tar.gz or a zip.", nameof(format));
            if (protection != ArchiveProtection.None && (passphrase == null || passphrase.Length == 0))
                throw new ArgumentException("A protected archive needs a passphrase.", nameof(passphrase));
            if (protection == ArchiveProtection.ZipAes256 && format != ArchiveFormat.Zip)
                throw new ArgumentException("Only a zip carries AES of its own.", nameof(protection));

            var listing = await collection.ListAsync(string.Empty, token);
            var entries = new List<ArchiveEntrySource>(listing.Count);

            foreach (var path in listing)
                if (IsManifest(path))
                    entries.Add(ArchiveEntrySource.FromStore(path, collection));
            foreach (var path in listing)
                if (!IsManifest(path))
                    entries.Add(ArchiveEntrySource.FromStore(path, collection));

            switch (protection)
            {
                case ArchiveProtection.None:
                    await PackPlainAsync(entries, destination, format, token);
                    return;

                case ArchiveProtection.ZipAes256:
                    await ZipService.PackEncryptedAsync(entries, destination, passphrase, token: token);
                    return;

                default:
                    var inner = FileNames.CollectionFileBaseName +
                                (format == ArchiveFormat.Zip ? FileNames.ZipExtension : FileNames.TarGzExtension);
                    await PgpSymmetricService.EncryptAsync(stream => PackPlainAsync(entries, stream, format, token),
                        destination, passphrase, PgpEncryptOptions.ForArchive(inner), token);
                    return;
            }
        }

        private static Task PackPlainAsync(IReadOnlyList<ArchiveEntrySource> entries, Stream destination,
            ArchiveFormat format, CancellationToken token) =>
            format == ArchiveFormat.Zip
                ? ZipService.PackAsync(entries, destination, token: token)
                : TarGzService.PackAsync(entries, destination, token: token);

        /// <summary> Unpacks an archive into <paramref name="into"/> and reads it as a collection. The
        /// stream must be seekable - it is sniffed, then read from the start. </summary>
        public static Task<(CollectionArchiveResult Result, CollectionContent Content)> UnpackAsync(
            Stream source, IContentStore into, SerializationService serialization, ArchiveLimits limits = null,
            CancellationToken token = default) =>
            UnpackAsync(source, into, serialization, null, limits, token);

        /// <summary> Unpacks an archive that may be encrypted. Without a passphrase an encrypted one answers
        /// <see cref="CollectionArchiveResult.PassphraseRequired"/>. </summary>
        public static async Task<(CollectionArchiveResult Result, CollectionContent Content)> UnpackAsync(
            Stream source, IContentStore into, SerializationService serialization, char[] passphrase,
            ArchiveLimits limits = null, CancellationToken token = default)
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
            if (format == ArchiveFormat.SevenZip)
                return (CollectionArchiveResult.Unsupported, null);

            if (format == ArchiveFormat.OpenPgp)
            {
                if (passphrase == null || passphrase.Length == 0) return (CollectionArchiveResult.PassphraseRequired, null);

                using var plaintext = new MemoryStream();
                var opened = await PgpSymmetricService.TryDecryptAsync(source, plaintext, passphrase,
                    limits.MaxTotalBytes, token);
                if (!opened.IsOk)
                    return opened.Result == PgpOpenResult.WrongPassphrase
                        ? (CollectionArchiveResult.WrongPassphrase, null)
                        : (CollectionArchiveResult.Damaged, null);

                // The plaintext is a container in its own right, and only it says which. A message inside
                // a message is refused rather than peeled without end.
                plaintext.Position = 0;
                var innerLeading = new byte[ArchiveFormatSniffer.MagicBytes];
                var innerRead = await plaintext.ReadAsync(innerLeading, 0, innerLeading.Length, token);
                if (innerRead < innerLeading.Length) Array.Resize(ref innerLeading, innerRead);
                plaintext.Position = 0;
                if (ArchiveFormatSniffer.Detect(innerLeading) == ArchiveFormat.OpenPgp)
                    return (CollectionArchiveResult.Damaged, null);

                return await UnpackAsync(plaintext, into, serialization, passphrase, limits, token);
            }

            if (format != ArchiveFormat.TarGz && format != ArchiveFormat.Zip)
                return (CollectionArchiveResult.NotAnArchive, null);

            try
            {
                if (format == ArchiveFormat.Zip)
                    await ZipService.UnpackAsync(source, into, limits, passphrase, token);
                else
                    await TarGzService.UnpackAsync(source, into, limits, token);
            }
            catch (InvalidDataException)
            {
                return (CollectionArchiveResult.Damaged, null);
            }
            catch (ArchivePassphraseRequiredException)
            {
                return (CollectionArchiveResult.PassphraseRequired, null);
            }
            catch (ArchiveWrongPassphraseException)
            {
                return (CollectionArchiveResult.WrongPassphrase, null);
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
