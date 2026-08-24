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

namespace BH.SDK.Services.Package
{
    // THIS IS ALSO THE BACKEND'S ENTRY POINT, and saying so changes what it may do. A client reads a
    // file the author picked; a server reads whatever an upload contained, which is to say whatever
    // somebody chose to send. So nothing here trusts a name, a length or a header: the archive layer
    // is handed limits, the store it unpacks into cannot address anything outside itself, and every
    // failure has an answer rather than an exception - a hostile upload is an ordinary Tuesday, not
    // an incident.
    //
    // WHAT IT IS IS SNIFFED FROM THE BYTES, NOT THE EXTENSION. A file arrives named .tar.gz.gpg, or
    // .gpg, or nothing at all, and the name is the one part of it anybody can write. Two magic
    // numbers answer the question: 1f 8b begins gzip, and an OpenPGP packet tag begins a message.
    //
    // THE PASSPHRASE HAS THREE ANSWERS, not two. "Not given" is a different situation from "wrong":
    // the first means the host should ask, the second means the person already answered and was
    // wrong. Collapsing them tells a player they got a password wrong before they typed one.

    /// <summary> Reads a level package back, from an archive or from a folder. </summary>
    public static class LevelPackageReader
    {
        /// <summary> Reads a package out of a stream - a .tar.gz or a .tar.gz.gpg. The stream must
        /// be seekable, since what it holds is decided by looking at its first bytes and then
        /// reading it from the start. </summary>
        public static async Task<LevelPackageContent> ReadAsync(Stream source, char[] passphrase = null,
            IContentStore unpackInto = null, ArchiveLimits limits = null, CancellationToken token = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!source.CanSeek)
                throw new ArgumentException("A package is sniffed and then re-read, so its stream must be seekable.",
                    nameof(source));

            limits = limits ?? ArchiveLimits.Default;

            var origin = source.Position;
            var leading = await PeekAsync(source, origin, token);

            if (IsGzip(leading))
            {
                source.Position = origin;
                return await UnpackAsync(source, unpackInto, limits, token);
            }

            if (!PgpSymmetricService.LooksLikeOpenPgp(leading))
                return LevelPackageContent.Failed(LevelPackageOpenResult.NotAPackage);

            if (passphrase == null || passphrase.Length == 0)
                return LevelPackageContent.Failed(LevelPackageOpenResult.PassphraseRequired);

            source.Position = origin;

            // Decrypted into memory rather than streamed onward, because what comes out has to be
            // read from its start by the tar reader and an OpenPGP stream cannot be rewound. The
            // decompression limit is what bounds this, the same way ArchiveLimits bounds the unpack
            // that follows.
            using var plaintext = new MemoryStream();
            var outcome = await PgpSymmetricService.TryDecryptAsync(source, plaintext, passphrase,
                limits.MaxTotalBytes, token);

            if (!outcome.IsOk) return LevelPackageContent.Failed(Translate(outcome.Result));

            plaintext.Position = 0;
            if (!IsGzip(await PeekAsync(plaintext, 0, token)))
                return LevelPackageContent.Failed(LevelPackageOpenResult.NotAPackage);

            plaintext.Position = 0;
            return await UnpackAsync(plaintext, unpackInto, limits, token);
        }

        /// <summary> Reads a package that is already a folder - the shape an export writes and the
        /// shape a level has on disk. </summary>
        public static async Task<LevelPackageContent> ReadAsync(IContentStore source,
            char[] passphrase = null, CancellationToken token = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var meta = await LocateAsync(source, FileNames.MetadataFileBaseName, token);
            var level = await LocateAsync(source, FileNames.LevelFileBaseName, token);

            if (!level.Found) return LevelPackageContent.Failed(LevelPackageOpenResult.NotAPackage);

            if (level.IsProtected && (passphrase == null || passphrase.Length == 0))
                return LevelPackageContent.Failed(LevelPackageOpenResult.PassphraseRequired);

            var levelBytes = await ReadAllAsync(source, level.Path, token);
            if (level.IsProtected)
            {
                using var encrypted = new MemoryStream(levelBytes, writable: false);
                using var plaintext = new MemoryStream();

                var outcome = await PgpSymmetricService.TryDecryptAsync(encrypted, plaintext, passphrase,
                    PgpSymmetricService.DefaultDecompressionLimit, token);

                if (!outcome.IsOk) return LevelPackageContent.Failed(Translate(outcome.Result));
                levelBytes = plaintext.ToArray();
            }

            // A missing metadata document is not a refusal: the level itself is what a package is
            // for, and a host can raise its own card out of the level. An absent one comes back as
            // null bytes rather than as an error nobody can act on.
            var metaBytes = meta.Found ? await ReadAllAsync(source, meta.Path, token) : null;

            return new LevelPackageContent(levelBytes, level.Format, level.IsProtected,
                metaBytes, meta.Format, source, await ResourcesAsync(source, token));
        }

        private static async Task<LevelPackageContent> UnpackAsync(Stream source, IContentStore unpackInto,
            ArchiveLimits limits, CancellationToken token)
        {
            // A store of the caller's choosing is what makes an import cheap: the host can unpack
            // straight into the new level's folder rather than through memory, and a server can
            // unpack into whatever it stores. Memory is only the default.
            var payload = unpackInto ?? new MemoryContentStore("package", limits.MaxTotalBytes);

            try
            {
                await TarGzService.UnpackAsync(source, payload, limits, token);
            }
            catch (InvalidDataException)
            {
                // Refused by one of the four checks, or over a cap - a package that cannot be
                // trusted, which from the outside is the same answer as one that is damaged.
                return LevelPackageContent.Failed(LevelPackageOpenResult.Damaged);
            }

            return await ReadAsync(payload, passphrase: null, token);
        }

        private static async Task<byte[]> PeekAsync(Stream source, long origin, CancellationToken token)
        {
            var leading = new byte[4];
            var read = await source.ReadAsync(leading, 0, leading.Length, token);
            source.Position = origin;

            if (read == leading.Length) return leading;

            var exact = new byte[read];
            Array.Copy(leading, exact, read);
            return exact;
        }

        private static bool IsGzip(byte[] leading) =>
            leading != null && leading.Length >= 2 && leading[0] == 0x1f && leading[1] == 0x8b;

        private static LevelPackageOpenResult Translate(PgpOpenResult result)
        {
            switch (result)
            {
                case PgpOpenResult.WrongPassphrase: return LevelPackageOpenResult.WrongPassphrase;
                case PgpOpenResult.Tampered: return LevelPackageOpenResult.Damaged;
                case PgpOpenResult.NotOpenPgp: return LevelPackageOpenResult.NotAPackage;
                default: return LevelPackageOpenResult.Unsupported;
            }
        }

        // Which format a document is in is decided by the NAME it was stored under, exactly as a
        // level folder on disk decides it - nothing inside the bytes says. The encrypted twin keeps
        // the inner extension for that reason (level.json.gpg), so the same probe answers both
        // questions at once.
        private static async Task<DocumentLocation> LocateAsync(IContentStore store, string baseName,
            CancellationToken token)
        {
            foreach (var format in new[] { SerializationType.Json, SerializationType.Bson })
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

        private readonly struct DocumentLocation
        {
            public readonly string Path;
            public readonly SerializationType Format;
            public readonly bool IsProtected;

            public DocumentLocation(string path, SerializationType format, bool isProtected)
            {
                Path = path;
                Format = format;
                IsProtected = isProtected;
            }

            public bool Found => !string.IsNullOrEmpty(Path);
        }
    }
}
