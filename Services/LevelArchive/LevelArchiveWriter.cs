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
    // Writing what LevelArchiveBuilder decided, in whichever of the four shapes the author picked.
    // The four are two independent choices - one file or a folder, protected or not - so this is two
    // methods rather than four, and the mode enum exists to give the pair a name in a dropdown.
    //
    // THE DOCUMENTS GO FIRST, ALWAYS, and it is the only mitigation tar.gz allows for what it cannot
    // do. A ZIP has a central directory, so metadata.json can be lifted out of a 50 MB archive
    // without touching the song; a stream format structurally cannot. Putting the two documents at
    // the front means a reader that only wants the level card decompresses a few kilobytes and
    // stops, which covers every listing and preview this project has. See TarGzService's header.
    //
    // WHAT "PROTECTED" MEANS DIFFERS BETWEEN THE TWO, and the difference is the point rather than an
    // inconsistency. A protected ARCHIVE is one .gpg file: nothing about it is readable, because it
    // is in transit and an archive in transit has no reason to advertise itself. A protected FOLDER
    // encrypts the level document ALONE - the metadata, the cover and the media stay plain, so the
    // level browser still renders a card with a name and a picture and asks for the passphrase only
    // when the level is opened. What is protected is the CONTENT, not the existence.

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

            // A PASSPHRASE ON ITS OWN MEANS PROTECTED, and this is not leniency. The options carry
            // WHICH scheme; handing one a passphrase says THAT it should be protected, and the only
            // other reading - write it in the clear and drop the passphrase on the floor - produces
            // a file the author believes is protected and is not. That failure is silent, survives
            // being sent to somebody, and is the worst outcome available at this line.
            //
            // WHICH scheme it means depends on the container, because only one of them can choose: a
            // zip carries AES of its own, which is what a password on an archive means by default -
            // the recipient opens it with the archiver they already have. A .tar.gz carries no
            // encryption at all, so there the answer is the OpenPGP layer or nothing.
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
                    // The archive is packed straight INTO the encrypted message rather than into a
                    // buffer first, so a big level never exists twice in memory. Its length is
                    // therefore unknown when the literal packet opens, which is exactly the
                    // streaming shape gpg itself writes.
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

            // THINNED LIKE ANY OTHER LEVEL WRITE. An archive's level document is a level.json by
            // another name, so a placement's materialized copies stay out of it and are rebuilt on
            // import (LevelArchiveGenerator). The host's own writes go through
            // BH.Core.Services.FileLoaderService, which does the same thing one line down; this path
            // never reaches it, and an archive holding the copies would be the one shape of this
            // format that still carried them.
            var levelBytes = serialization.SerializeEnvelope(
                PrefabVirtualizationUtils.Thin(plan.Level), options.LevelFormat);

            if (passphrase == null || passphrase.Length == 0)
            {
                entries.Add(ArchiveEntrySource.FromBytes(levelName, levelBytes));
                return entries;
            }

            // Buffered rather than streamed, and this is the one place where that is right: a level
            // document is the small half of an archive, it is already fully in memory as the byte[]
            // the serializer just produced, and knowing its length lets the message use
            // definite-length packets - the shape every reader handles.
            using var buffer = new MemoryStream();
            await PgpSymmetricService.EncryptBytesAsync(levelBytes, buffer, passphrase,
                PgpEncryptOptions.ForDocument(levelName), token);

            entries.Add(ArchiveEntrySource.FromBytes(levelName + FileNames.EncryptedExtension,
                buffer.ToArray()));

            return entries;
        }

        // A collected file is the one thing an archive carries that does not live in the level's own
        // store, so it is the one thing opened by absolute path - and the opener is built HERE
        // rather than in the archive layer, which must stay unable to address anything outside a
        // rooted store. See ArchiveEntrySource's header.
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