using System;
using Org.BouncyCastle.Bcpg;

namespace BH.SDK.Services.Crypto
{
    // What a message says about itself, and one number that is a real trade rather than a setting.
    //
    // S2K WORK IS COUNTED IN BYTES HASHED, NOT ITERATIONS, and gpg's own default asks for roughly
    // 65 MB of SHA-256 - a fifth of a second on a desktop and potentially two seconds on a cheap
    // phone, every time a protected level is opened. The default here is a quarter of that, and the
    // reason it is defensible is what this protects: a level file already sitting on the player's
    // own disk, guarded by a password its author chose to share with somebody. It is stated in the
    // format's own docs that this is not DRM. A file gpg wrote with a higher count still opens -
    // reading takes whatever the file declares - so nothing about interoperability rests on this.
    //
    // THE MODIFICATION TIME IS PINNED. A literal-data packet carries one, and the local clock is
    // not something an exported level should be leaking; a fixed instant also means two exports of
    // the same document differ only where OpenPGP genuinely randomizes (salt and IV).

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
        /// a package reader tells a level.json from a level.bson. </summary>
        public string InnerFileName { get; set; } = string.Empty;

        // Zip for a level document, which is JSON and compresses several times over; Uncompressed
        // for a .tar.gz, whose payload is already deflated and would only be made slightly larger
        // by a second pass.

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

        /// <summary> Options for a level document - text, so it is compressed. </summary>
        public static PgpEncryptOptions ForDocument(string innerFileName) =>
            new PgpEncryptOptions
            {
                InnerFileName = innerFileName,
                Compression = CompressionAlgorithmTag.Zip,
            };

        /// <summary> Options for a package - a .tar.gz, already deflated, so compressing it again
        /// would only make it slightly larger. </summary>
        public static PgpEncryptOptions ForArchive(string innerFileName) =>
            new PgpEncryptOptions
            {
                InnerFileName = innerFileName,
                Compression = CompressionAlgorithmTag.Uncompressed,
            };
    }
}