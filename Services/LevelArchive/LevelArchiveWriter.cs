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
using BH.SDK.Utils;

namespace BH.SDK.Services.LevelArchive
{
    // Writes what LevelArchiveBuilder decided: two independent choices (file or folder, protected or
    // not), so two methods. Documents go FIRST (tar.gz has no central directory). A protected archive
    // hides everything; a protected FOLDER encrypts the level document alone. Docs/ARCHIVE.md.

    /// <summary> Writes a planned level archive out. </summary>
    public static class LevelArchiveWriter
    {
        private const int CopyBufferSize = 81920;

        /// <summary> Writes the archive as a folder - the same shape a level has on disk. Returns
        /// what was written, so a host can report it. </summary>
        public static async Task<IReadOnlyList<string>> WriteFolderAsync(LevelArchivePlan plan,
            IContentStore source, IContentStore target, SerializationService serialization,
            LevelArchiveOptions options = null, char[] passphrase = null,
            CancellationToken token = default)
        {
            Require(plan, source, serialization);
            if (target == null) throw new ArgumentNullException(nameof(target));

            options = options ?? LevelArchiveOptions.Default;

            var entries = await DocumentsAsync(plan, serialization, options, passphrase, token);
            foreach (var file in plan.Files) entries.Add(ToEntry(file, source));

            var written = new List<string>(entries.Count);
            foreach (var entry in entries)
            {
                token.ThrowIfCancellationRequested();

                using (var output = await target.OpenWriteAsync(entry.Path, token))
                using (var content = await entry.OpenReadAsync(token))
                    await content.CopyToAsync(output, CopyBufferSize, token);

                written.Add(entry.Path);
            }

            return written;
        }

        /// <summary> Writes the archive as one file - a .tar.gz or a .zip, in the clear, behind an
        /// OpenPGP passphrase, or with the zip's own AES. </summary>
        public static async Task WriteArchiveAsync(LevelArchivePlan plan, IContentStore source,
            Stream destination, SerializationService serialization, LevelArchiveOptions options = null,
            char[] passphrase = null, CancellationToken token = default)
        {
            Require(plan, source, serialization);
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            options = options ?? LevelArchiveOptions.Default;

            var hasPassphrase = passphrase != null && passphrase.Length > 0;
            var protection = options.Protection;

            // A passphrase on its own means PROTECTED: dropping it would yield a file the author believes
            // is protected and is not. Which scheme depends on the container: a zip carries AES, a
            // .tar.gz has none, so OpenPGP or nothing.
            if (protection == ArchiveProtection.None && hasPassphrase)
                protection = options.Format == ArchiveFormat.Zip
                    ? ArchiveProtection.ZipAes256
                    : ArchiveProtection.OpenPgp;

            if (protection != ArchiveProtection.None && !hasPassphrase)
                throw new ArgumentException("A protected archive needs a passphrase.", nameof(passphrase));

            // An impossible combination, refused by the type rather than degraded into something
            // the author did not ask for: tar.gz has no encryption of its own, so AES inside it is
            // not a weaker shape of the same request - it is not a shape at all.
            if (protection == ArchiveProtection.ZipAes256 && options.Format != ArchiveFormat.Zip)
                throw new ArgumentException("Only a zip carries AES of its own.", nameof(options));

            // Never encrypted individually here: the whole archive is what a passphrase covers in
            // this shape, and encrypting the document twice would only make it unreadable to
            // whoever already holds the passphrase for the outer layer.
            var entries = await DocumentsAsync(plan, serialization, options, passphrase: null, token);
            foreach (var file in plan.Files) entries.Add(ToEntry(file, source));

            switch (protection)
            {
                case ArchiveProtection.None:
                    await PackAsync(entries, destination, options.Format, token);
                    return;

                case ArchiveProtection.ZipAes256:
                    await ZipService.PackEncryptedAsync(entries, destination, passphrase,
                        ArchivePolicy.Default, token);
                    return;

                default:
                    // Packed straight INTO the encrypted message so a big level never exists twice in
                    // memory (the length is unknown when the literal packet opens, as gpg writes it).
                    await PgpSymmetricService.EncryptAsync(
                        stream => PackAsync(entries, stream, options.Format, token),
                        destination, passphrase,
                        PgpEncryptOptions.ForArchive(FileNames.LevelFileBaseName + ExtensionOf(options.Format)),
                        token);
                    return;
            }
        }

        private static Task PackAsync(IReadOnlyList<ArchiveEntrySource> entries, Stream destination,
            ArchiveFormat format, CancellationToken token) =>
            format == ArchiveFormat.Zip
                ? ZipService.PackAsync(entries, destination, ArchivePolicy.Default, token)
                : TarGzService.PackAsync(entries, destination, ArchivePolicy.Default, token);

        // The name inside the PGP literal packet, which is what `gpg -d` suggests when it writes the
        // plaintext out. It has to name the container that is actually in there, or the file gpg
        // hands a player back is a zip called .tar.gz.
        private static string ExtensionOf(ArchiveFormat format) =>
            format == ArchiveFormat.Zip ? FileNames.ZipExtension : FileNames.TarGzExtension;

        // The two documents, in the order they are written. A protected folder swaps the level's
        // entry for its encrypted twin and leaves the metadata alone - see this file's header.
        private static async Task<List<ArchiveEntrySource>> DocumentsAsync(LevelArchivePlan plan,
            SerializationService serialization, LevelArchiveOptions options, char[] passphrase,
            CancellationToken token)
        {
            var metaName = FileNames.MetadataFileBaseName + options.MetaFormat.ToFileExtension();
            var levelName = FileNames.LevelFileBaseName + options.LevelFormat.ToFileExtension();

            var entries = new List<ArchiveEntrySource>(2)
            {
                ArchiveEntrySource.FromBytes(metaName, serialization.SerializeEnvelope(plan.Meta, options.MetaFormat)),
            };

            // Thinned like any other level write: a placement's materialized copies stay out and are
            // rebuilt on import (LevelArchiveGenerator); the host's FileLoaderService is not reached here.
            var levelBytes = serialization.SerializeEnvelope(
                PrefabVirtualizationUtils.Thin(plan.Level), options.LevelFormat);

            if (passphrase == null || passphrase.Length == 0)
            {
                entries.Add(ArchiveEntrySource.FromBytes(levelName, levelBytes));
                return entries;
            }

            // Buffered: the document is small and already a byte[], and a known length gives definite-length
            // packets, the shape every reader handles.
            using var buffer = new MemoryStream();
            await PgpSymmetricService.EncryptBytesAsync(levelBytes, buffer, passphrase,
                PgpEncryptOptions.ForDocument(levelName), token);

            entries.Add(ArchiveEntrySource.FromBytes(levelName + FileNames.EncryptedExtension,
                buffer.ToArray()));

            return entries;
        }

        // The one thing opened by absolute path; the opener is built HERE because the archive layer must
        // stay unable to address anything outside a rooted store.
        private static ArchiveEntrySource ToEntry(ArchiveFile file, IContentStore source)
        {
            if (!file.IsExternal) return ArchiveEntrySource.FromStore(file.ArchivePath, source, file.SourcePath);

            return ArchiveEntrySource.FromOpener(file.ArchivePath,
                _ => new ValueTask<Stream>(new FileStream(file.SourcePath, FileMode.Open, FileAccess.Read,
                    FileShare.Read, CopyBufferSize, useAsync: true)),
                _ => new ValueTask<long>(new FileInfo(file.SourcePath).Length));
        }

        private static void Require(LevelArchivePlan plan, IContentStore source, SerializationService serialization)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (serialization == null) throw new ArgumentNullException(nameof(serialization));
        }
    }
}