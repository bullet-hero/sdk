using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BH.SDK.Models;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Services.Archive;
using BH.SDK.Services.Content;
using BH.SDK.Services.Crypto;

namespace BH.SDK.Services.LevelArchive
{
    // THIS IS ALSO THE BACKEND'S ENTRY POINT, and saying so changes what it may do. A client reads a
    // file the author picked; a server reads whatever an upload contained, which is to say whatever
    // somebody chose to send. So nothing here trusts a name, a length or a header: the archive layer
    // is handed limits, the store it unpacks into cannot address anything outside itself, and every
    // failure has an answer rather than an exception - a hostile upload is an ordinary Tuesday, not
    // an incident.
    //
    // WHAT IT IS IS SNIFFED FROM THE BYTES, NOT THE EXTENSION. A file arrives named .tar.gz.gpg, or
    // .zip, or .gpg, or nothing at all, and the name is the one part of it anybody can write.
    // ArchiveFormatSniffer answers instead, and a PROTECTED archive is asked TWICE: the outer layer
    // is an OpenPGP message either way, and only its plaintext says whether a tar.gz or a zip was
    // inside. 7z is recognised for one purpose - so a refusal can name the format rather than call
    // a perfectly good file corrupt.
    //
    // THE PASSPHRASE HAS THREE ANSWERS, not two. "Not given" is a different situation from "wrong":
    // the first means the host should ask, the second means the person already answered and was
    // wrong. Collapsing them tells a player they got a password wrong before they typed one.

    /// <summary> Reads a level archive back, from an archive or from a folder. </summary>
    public static class LevelArchiveReader
    {
        /// <summary> Reads an archive out of a stream - a .tar.gz, a .zip, or either of them
        /// protected. The stream must be seekable, since what it holds is decided by looking at its
        /// first bytes and then reading it from the start. </summary>
        public static async Task<LevelArchiveContent> ReadAsync(Stream source, char[] passphrase = null,
            IContentStore unpackInto = null, ArchiveLimits limits = null, CancellationToken token = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!source.CanSeek)
                throw new ArgumentException("An archive is sniffed and then re-read, so its stream must be seekable.",
                    nameof(source));

            limits = limits ?? ArchiveLimits.Default;

            var origin = source.Position;
            var leading = await PeekAsync(source, origin, token);

            var format = ArchiveFormatSniffer.Detect(leading);

            // 7z is refused BY NAME and without being opened, which is the whole of what this build
            // does about the format. "This game cannot read 7z yet" sends a person to re-zip their
            // file; "not an archive" sends them looking for a corruption that is not there.
            if (format == ArchiveFormat.SevenZip)
                return LevelArchiveContent.Failed(LevelArchiveOpenResult.Unsupported, format);

            if (format == ArchiveFormat.TarGz || format == ArchiveFormat.Zip)
            {
                source.Position = origin;
                return await UnpackAsync(source, format, unpackInto, limits, passphrase, token);
            }

            if (format != ArchiveFormat.OpenPgp)
                return LevelArchiveContent.Failed(LevelArchiveOpenResult.NotAnArchive, format);

            if (passphrase == null || passphrase.Length == 0)
                return LevelArchiveContent.Failed(LevelArchiveOpenResult.PassphraseRequired, format);

            source.Position = origin;

            // Decrypted into memory rather than streamed onward, because what comes out has to be
            // read from its start by the tar reader and an OpenPGP stream cannot be rewound. The
            // decompression limit is what bounds this, the same way ArchiveLimits bounds the unpack
            // that follows.
            using var plaintext = new MemoryStream();
            var outcome = await PgpSymmetricService.TryDecryptAsync(source, plaintext, passphrase,
                limits.MaxTotalBytes, token);

            if (!outcome.IsOk) return LevelArchiveContent.Failed(Translate(outcome.Result), format);

            // SNIFFED AGAIN, because what comes out of the message is a container in its own right
            // and it is not always the same one: a .tar.gz.gpg and a .zip.gpg are both ordinary
            // OpenPGP messages, and only the plaintext says which. Asking once and assuming gzip is
            // what made a protected zip read as "not an archive".
            plaintext.Position = 0;
            var inner = ArchiveFormatSniffer.Detect(await PeekAsync(plaintext, 0, token));

            if (inner == ArchiveFormat.SevenZip)
                return LevelArchiveContent.Failed(LevelArchiveOpenResult.Unsupported, inner);

            if (inner != ArchiveFormat.TarGz && inner != ArchiveFormat.Zip)
                return LevelArchiveContent.Failed(LevelArchiveOpenResult.NotAnArchive, inner);

            plaintext.Position = 0;

            // No passphrase downwards: the message WAS the protection, and a zip inside it is in
            // the clear by construction.
            return await UnpackAsync(plaintext, inner, unpackInto, limits, passphrase: null, token);
        }

        /// <summary> Reads an archive that is already a folder - the shape an export writes and the
        /// shape a level has on disk. </summary>
        public static async Task<LevelArchiveContent> ReadAsync(IContentStore source,
            char[] passphrase = null, CancellationToken token = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var meta = await LocateAsync(source, FileNames.MetadataFileBaseName, token);
            var level = await LocateAsync(source, FileNames.LevelFileBaseName, token);

            if (!level.Found) return LevelArchiveContent.Failed(LevelArchiveOpenResult.NotAnArchive);

            if (level.IsProtected && (passphrase == null || passphrase.Length == 0))
                return LevelArchiveContent.Failed(LevelArchiveOpenResult.PassphraseRequired);

            var levelBytes = await ReadAllAsync(source, level.Path, token);
            if (level.IsProtected)
            {
                using var encrypted = new MemoryStream(levelBytes, writable: false);
                using var plaintext = new MemoryStream();

                var outcome = await PgpSymmetricService.TryDecryptAsync(encrypted, plaintext, passphrase,
                    PgpSymmetricService.DefaultDecompressionLimit, token);

                if (!outcome.IsOk) return LevelArchiveContent.Failed(Translate(outcome.Result));
                levelBytes = plaintext.ToArray();
            }

            // A missing metadata document is not a refusal: the level itself is what an archive is
            // for, and a host can raise its own card out of the level. An absent one comes back as
            // null bytes rather than as an error nobody can act on.
            var metaBytes = meta.Found ? await ReadAllAsync(source, meta.Path, token) : null;

            return new LevelArchiveContent(levelBytes, level.Format, level.IsProtected,
                metaBytes, meta.Format, source, await ResourcesAsync(source, token));
        }

        // WHAT AN ARCHIVE IS, WITHOUT OPENING IT. Both halves of the answer are cheap for different
        // reasons: a zip is read out of its central directory, so the song is never touched at all,
        // and a tar.gz is read from the front, which works only because the writer puts the two
        // documents first. A protected archive answers PassphraseRequired without being decrypted -
        // the metadata of a .gpg is inside the encryption, by design.

        /// <summary> Just the metadata document, for a host that wants to name what it is about to
        /// import. Null bytes when the archive carries none. </summary>
        public static async Task<LevelArchivePeek> PeekMetaAsync(Stream source,
            ArchiveLimits limits = null, CancellationToken token = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!source.CanSeek)
                throw new ArgumentException("An archive is sniffed and then re-read, so its stream must be seekable.",
                    nameof(source));

            limits = limits ?? ArchiveLimits.Default;

            var origin = source.Position;
            var format = ArchiveFormatSniffer.Detect(await PeekAsync(source, origin, token));

            if (format == ArchiveFormat.SevenZip)
                return new LevelArchivePeek(format, LevelArchiveOpenResult.Unsupported, null, default, 0);

            if (format == ArchiveFormat.OpenPgp)
                return new LevelArchivePeek(format, LevelArchiveOpenResult.PassphraseRequired, null, default, 0);

            if (format != ArchiveFormat.TarGz && format != ArchiveFormat.Zip)
                return new LevelArchivePeek(format, LevelArchiveOpenResult.NotAnArchive, null, default, 0);

            var wanted = new List<string>(SerializationTypeExtensions.ProbeOrder.Count);
            foreach (var probe in SerializationTypeExtensions.ProbeOrder)
                wanted.Add(FileNames.MetadataFileBaseName + probe.ToFileExtension());

            try
            {
                source.Position = origin;

                var entries = format == ArchiveFormat.Zip
                    ? await ZipService.ReadEntriesAsync(source, wanted, limits, null, token)
                    : await TarGzService.ReadLeadingAsync(source, wanted, limits, token);

                source.Position = origin;
                var count = format == ArchiveFormat.Zip ? CountZipEntries(source, limits) : 0;

                foreach (var probe in SerializationTypeExtensions.ProbeOrder)
                {
                    var name = FileNames.MetadataFileBaseName + probe.ToFileExtension();
                    if (entries.TryGetValue(name, out var bytes))
                        return new LevelArchivePeek(format, LevelArchiveOpenResult.Ok, bytes, probe, count);
                }

                // A missing metadata document is not a refusal here either - the archive may still
                // hold a level, and only a real read can say.
                return new LevelArchivePeek(format, LevelArchiveOpenResult.Ok, null, default, count);
            }
            catch (InvalidDataException)
            {
                return new LevelArchivePeek(format, LevelArchiveOpenResult.Damaged, null, default, 0);
            }
            catch (ArchivePassphraseRequiredException)
            {
                return new LevelArchivePeek(format, LevelArchiveOpenResult.PassphraseRequired, null, default, 0);
            }
        }

        private static int CountZipEntries(Stream source, ArchiveLimits limits)
        {
            try
            {
                return ZipService.List(source, out _, limits).Count;
            }
            catch (InvalidDataException)
            {
                return 0;
            }
        }

        private static async Task<LevelArchiveContent> UnpackAsync(Stream source, ArchiveFormat format,
            IContentStore unpackInto, ArchiveLimits limits, char[] passphrase, CancellationToken token)
        {
            // A store of the caller's choosing is what makes an import cheap: the host can unpack
            // straight into the new level's folder rather than through memory, and a server can
            // unpack into whatever it stores. Memory is only the default.
            var payload = unpackInto ?? new MemoryContentStore("archive", limits.MaxTotalBytes);

            try
            {
                if (format == ArchiveFormat.Zip)
                    await ZipService.UnpackAsync(source, payload, limits, passphrase, token);
                else
                    await TarGzService.UnpackAsync(source, payload, limits, token);
            }
            catch (InvalidDataException)
            {
                // Refused by one of the four checks, or over a cap - an archive that cannot be
                // trusted, which from the outside is the same answer as one that is damaged.
                return LevelArchiveContent.Failed(LevelArchiveOpenResult.Damaged, format);
            }
            catch (ArchivePassphraseRequiredException)
            {
                // A zip can be encrypted ENTRY BY ENTRY, which nothing outside it advertises - so
                // unlike a .gpg, this is the first moment the question can be asked at all.
                return LevelArchiveContent.Failed(LevelArchiveOpenResult.PassphraseRequired, format);
            }
            catch (ArchiveWrongPassphraseException)
            {
                return LevelArchiveContent.Failed(LevelArchiveOpenResult.WrongPassphrase, format);
            }

            var content = await ReadAsync(payload, passphrase: null, token);
            return content.IsOk
                ? new LevelArchiveContent(content.LevelBytes, content.LevelFormat, content.LevelWasProtected,
                    content.MetaBytes, content.MetaFormat, content.Payload, content.ResourceFileNames, format)
                : LevelArchiveContent.Failed(content.Result, format);
        }

        private static async Task<byte[]> PeekAsync(Stream source, long origin, CancellationToken token)
        {
            var leading = new byte[ArchiveFormatSniffer.MagicBytes];
            var read = await source.ReadAsync(leading, 0, leading.Length, token);
            source.Position = origin;

            if (read == leading.Length) return leading;

            var exact = new byte[read];
            Array.Copy(leading, exact, read);
            return exact;
        }

        private static LevelArchiveOpenResult Translate(PgpOpenResult result)
        {
            switch (result)
            {
                case PgpOpenResult.WrongPassphrase: return LevelArchiveOpenResult.WrongPassphrase;
                case PgpOpenResult.Tampered: return LevelArchiveOpenResult.Damaged;
                case PgpOpenResult.NotOpenPgp: return LevelArchiveOpenResult.NotAnArchive;
                default: return LevelArchiveOpenResult.Unsupported;
            }
        }

        // Which format a document is in is decided by the NAME it was stored under, exactly as a
        // level folder on disk decides it - nothing inside the bytes says. The encrypted twin keeps
        // the inner extension for that reason (level.json.gpg), so the same probe answers both
        // questions at once.
        private static async Task<DocumentLocation> LocateAsync(IContentStore store, string baseName,
            CancellationToken token)
        {
            foreach (var format in SerializationTypeExtensions.ProbeOrder)
            {
                var plain = baseName + format.ToFileExtension();
                if (await store.ExistsAsync(plain, token))
                    return new DocumentLocation(plain, format, isProtected: false);

                var encrypted = plain + FileNames.EncryptedExtension;
                if (await store.ExistsAsync(encrypted, token))
                    return new DocumentLocation(encrypted, format, isProtected: true);
            }

            return default;
        }

        private static async Task<byte[]> ReadAllAsync(IContentStore store, string path, CancellationToken token)
        {
            using var content = await store.OpenReadAsync(path, token);
            using var buffer = new MemoryStream();

            await content.CopyToAsync(buffer, 81920, token);
            return buffer.ToArray();
        }

        private static async Task<IReadOnlyList<string>> ResourcesAsync(IContentStore store, CancellationToken token)
        {
            var listing = await store.ListAsync(string.Empty, token);

            var resources = new List<string>(listing.Count);
            foreach (var path in listing)
                if (!IsDocumentName(path))
                    resources.Add(path);

            return resources;
        }

        private static bool IsDocumentName(string path)
        {
            if (path.EndsWith(FileNames.EncryptedExtension, StringComparison.Ordinal))
                path = path.Substring(0, path.Length - FileNames.EncryptedExtension.Length);

            if (path.IndexOf(ContentPath.Separator) >= 0) return false;

            var stem = Path.GetFileNameWithoutExtension(path);
            return string.Equals(stem, FileNames.LevelFileBaseName, StringComparison.Ordinal)
                   || string.Equals(stem, FileNames.MetadataFileBaseName, StringComparison.Ordinal);
        }

        /// <summary> Where an archive's level document turned out to be, and in which format - including whether it
        /// arrived encrypted. </summary>
        private readonly struct DocumentLocation
        {
            /// <summary> Where the document turned out to be. </summary>
            public readonly string Path;

            /// <summary> Which format it is in. </summary>
            public readonly SerializationType Format;

            /// <summary> Whether it arrived encrypted. </summary>
            public readonly bool IsProtected;

            /// <summary> Built from its path, format and protected. </summary>
            public DocumentLocation(string path, SerializationType format, bool isProtected)
            {
                Path = path;
                Format = format;
                IsProtected = isProtected;
            }

            /// <summary> True when the archive carried one at all. </summary>
            public bool Found => !string.IsNullOrEmpty(Path);
        }
    }
}