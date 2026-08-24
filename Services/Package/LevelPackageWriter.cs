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

namespace BH.SDK.Services.Package
{
    // Writing what LevelPackageBuilder decided, in whichever of the four shapes the author picked.
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
    // is in transit and a package in transit has no reason to advertise itself. A protected FOLDER
    // encrypts the level document ALONE - the metadata, the cover and the media stay plain, so the
    // level browser still renders a card with a name and a picture and asks for the passphrase only
    // when the level is opened. What is protected is the CONTENT, not the existence.

    /// <summary> Writes a planned level package out. </summary>
    public static class LevelPackageWriter
    {
        private const int CopyBufferSize = 81920;

        /// <summary> Writes the package as a folder - the same shape a level has on disk. Returns
        /// what was written, so a host can report it. </summary>
        public static async Task<IReadOnlyList<string>> WriteFolderAsync(LevelPackagePlan plan,
            IContentStore source, IContentStore target, SerializationService serialization,
            LevelPackageOptions options = null, char[] passphrase = null,
            CancellationToken token = default)
        {
            Require(plan, source, serialization);
            if (target == null) throw new ArgumentNullException(nameof(target));

            options = options ?? LevelPackageOptions.Default;

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

        /// <summary> Writes the package as one .tar.gz, or - given a passphrase - one
        /// .tar.gz.gpg. </summary>
        public static async Task WriteArchiveAsync(LevelPackagePlan plan, IContentStore source,
            Stream destination, SerializationService serialization, LevelPackageOptions options = null,
            char[] passphrase = null, CancellationToken token = default)
        {
            Require(plan, source, serialization);
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            options = options ?? LevelPackageOptions.Default;

            // Never encrypted individually here: the whole archive is what a passphrase covers in
            // this shape, and encrypting the document twice would only make it unreadable to
            // whoever already holds the passphrase for the outer layer.
            var entries = await DocumentsAsync(plan, serialization, options, passphrase: null, token);
            foreach (var file in plan.Files) entries.Add(ToEntry(file, source));

            if (passphrase == null || passphrase.Length == 0)
            {
                await TarGzService.PackAsync(entries, destination, ArchivePolicy.Default, token);
                return;
            }

            // The archive is packed straight INTO the encrypted message rather than into a buffer
            // first, so a big level never exists twice in memory. Its length is therefore unknown
            // when the literal packet opens, which is exactly the streaming shape gpg itself writes.
            await PgpSymmetricService.EncryptAsync(
                stream => TarGzService.PackAsync(entries, stream, ArchivePolicy.Default, token),
                destination, passphrase,
                PgpEncryptOptions.ForArchive(FileNames.LevelFileBaseName + FileNames.PackageExtension),
                token);
        }

        // The two documents, in the order they are written. A protected folder swaps the level's
        // entry for its encrypted twin and leaves the metadata alone - see this file's header.
        private static async Task<List<ArchiveEntrySource>> DocumentsAsync(LevelPackagePlan plan,
            SerializationService serialization, LevelPackageOptions options, char[] passphrase,
            CancellationToken token)
        {
            var metaName = FileNames.MetadataFileBaseName + options.MetaFormat.ToFileExtension();
            var levelName = FileNames.LevelFileBaseName + options.LevelFormat.ToFileExtension();

            var entries = new List<ArchiveEntrySource>(2)
            {
                ArchiveEntrySource.FromBytes(metaName, serialization.SerializeEnvelope(plan.Meta, options.MetaFormat)),
            };

            var levelBytes = serialization.SerializeEnvelope(plan.Level, options.LevelFormat);

            if (passphrase == null || passphrase.Length == 0)
            {
                entries.Add(ArchiveEntrySource.FromBytes(levelName, levelBytes));
                return entries;
            }

            // Buffered rather than streamed, and this is the one place where that is right: a level
            // document is the small half of a package, it is already fully in memory as the byte[]
            // the serializer just produced, and knowing its length lets the message use
            // definite-length packets - the shape every reader handles.
            using var buffer = new MemoryStream();
            await PgpSymmetricService.EncryptBytesAsync(levelBytes, buffer, passphrase,
                PgpEncryptOptions.ForDocument(levelName), token);

            entries.Add(ArchiveEntrySource.FromBytes(levelName + FileNames.EncryptedExtension,
                buffer.ToArray()));

            return entries;
        }

        // A collected file is the one thing a package carries that does not live in the level's own
        // store, so it is the one thing opened by absolute path - and the opener is built HERE
        // rather than in the archive layer, which must stay unable to address anything outside a
        // rooted store. See ArchiveEntrySource's header.
        private static ArchiveEntrySource ToEntry(PackageFile file, IContentStore source)
        {
            if (!file.IsExternal) return ArchiveEntrySource.FromStore(file.PackagePath, source, file.SourcePath);

            return ArchiveEntrySource.FromOpener(file.PackagePath,
                _ => new ValueTask<Stream>(new FileStream(file.SourcePath, FileMode.Open, FileAccess.Read,
                    FileShare.Read, CopyBufferSize, useAsync: true)),
                _ => new ValueTask<long>(new FileInfo(file.SourcePath).Length));
        }

        private static void Require(LevelPackagePlan plan, IContentStore source, SerializationService serialization)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (serialization == null) throw new ArgumentNullException(nameof(serialization));
        }
    }
}
