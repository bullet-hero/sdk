using System;
using Org.BouncyCastle.Bcpg;

namespace BH.SDK.Services.Crypto
{
    // What a message says about itself. S2K work is counted in BYTES hashed (default a quarter of gpg's:
    // this protects a file on the player's own disk, not DRM) and the modification time is PINNED, so the
    // local clock does not leak. Reading takes whatever the file declares. Docs/ARCHIVE.md.

    /// <summary> How an OpenPGP message is written. </summary>
    public sealed class PgpEncryptOptions
    {
        /// <summary> What a caller gets by asking for nothing in particular. </summary>
        public static PgpEncryptOptions Default { get; } = new PgpEncryptOptions();

        /// <summary> The instant every literal-data packet is stamped with. </summary>
        public static readonly DateTime PinnedModTime = new DateTime(1980, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary> Bytes of SHA-256 the passphrase is stretched through. </summary>
        public const int DefaultS2KWorkBytes = 16 * 1024 * 1024;

        /// <summary> The name recorded inside the message - what gpg restores the file as, and how
        /// an archive reader tells a level.json from a level.blob. </summary>
        public string InnerFileName { get; set; } = string.Empty;

        // Zip for a level document, which compresses several times over in either format - JSON is
        // text, and a blob is mostly repeated little-endian numbers; Uncompressed for a .tar.gz,
        // whose payload is already deflated and would only be made slightly larger by a second pass.

        /// <summary> Compression applied INSIDE the encrypted message. </summary>
        public CompressionAlgorithmTag Compression { get; set; } = CompressionAlgorithmTag.Zip;

        /// <summary> Bytes of hashing the passphrase is stretched through. OpenPGP stores this as
        /// one coded octet, so it is rounded UP to the nearest rung the format can express - see
        /// <see cref="PgpSymmetricService.ToCodedS2K"/>. </summary>
        public int S2KWorkBytes { get; set; } = DefaultS2KWorkBytes;

        /// <summary> Length of the plaintext when the caller knows it, which lets the message use
        /// definite-length packets - the most widely readable shape. Negative means "unknown",
        /// and the message streams with partial lengths instead, which is what gpg itself writes
        /// when it is piped into. </summary>
        public long PlaintextLength { get; set; } = -1;

        // There are exactly two things this format is ever asked to wrap, and naming them is what
        // keeps BouncyCastle's own enum out of every caller. It is still settable for anything
        // unforeseen; nothing in this project sets it.

        /// <summary> Options for a level document - Json or Blob, both worth compressing. </summary>
        public static PgpEncryptOptions ForDocument(string innerFileName) =>
            new PgpEncryptOptions
            {
                InnerFileName = innerFileName,
                Compression = CompressionAlgorithmTag.Zip,
            };

        /// <summary> Options for an archive - a .tar.gz, already deflated, so compressing it again
        /// would only make it slightly larger. </summary>
        public static PgpEncryptOptions ForArchive(string innerFileName) =>
            new PgpEncryptOptions
            {
                InnerFileName = innerFileName,
                Compression = CompressionAlgorithmTag.Uncompressed,
            };
    }
}