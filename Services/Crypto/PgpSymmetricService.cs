using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities.IO;

namespace BH.SDK.Services.Crypto
{
    // Exactly what `gpg --symmetric --cipher-algo AES256` produces, and that sentence is the entire
    // specification: SKESK v4, S2K type 3 (iterated and salted) over SHA-256, SEIPD v1 (CFB with an
    // MDC) under AES-256, and a literal-data packet inside. A level protected by this game opens in
    // gpg, and one gpg protected opens in this game. Nothing about the layout is ours to choose.
    //
    // SEIPD v1 RATHER THAN RFC 9580's v2/AEAD, and this is a decision that looks backwards until
    // the reason is stated: GnuPG does not implement the new formats - its developers went to
    // LibrePGP instead - and gpg is the external tool the whole format was picked for. Writing v2
    // would mean writing something the one program this has to interoperate with cannot read. v1 is
    // read by gpg, Sequoia, OpenPGP.js and GopenPGP alike. When that changes, the reader already
    // answers Unsupported with a clear message rather than failing obscurely.
    //
    // THE Utf8 OVERLOADS ARE MANDATORY AND IT IS NOT A STYLE POINT. AddMethod/GetDataStream encode
    // a passphrase one byte per char, while gpg treats it as UTF-8. For an ASCII passphrase the two
    // agree; for a Cyrillic one a file this game wrote will not open in gpg and a file gpg wrote
    // will not open here. The audience is Russian-speaking, so that is the DEFAULT case, not an
    // edge one - and it is silent, since both sides simply report a wrong passphrase.
    //
    // Verify() is always called, and it is what separates "wrong passphrase" from "damaged file" -
    // two different sentences to show a player, and the practical reason encrypt-then-authenticate
    // earns its place here at all.
    //
    // ASCII armor (.asc) is deliberately not written: a third more bytes to gain a shape nothing in
    // this project transports as text.

    /// <summary> Symmetric OpenPGP - the format a password-protected level is written in. </summary>
    public static class PgpSymmetricService
    {
        private const int CopyBufferSize = 81920;

        /// <summary> Most a message is allowed to decompress to before the read is abandoned. The
        /// counterpart of ArchiveLimits: an OpenPGP message carries a compressed packet, so it can
        /// carry a decompression bomb exactly like an archive can. </summary>
        public const long DefaultDecompressionLimit = 1024L * 1024 * 1024;

        /// <summary> Whether these leading bytes begin an OpenPGP message. Reads the packet tag
        /// rather than a magic number, since OpenPGP has none - which is why this answers "looks
        /// like" and the real answer comes from trying to open it. </summary>
        public static bool LooksLikeOpenPgp(byte[] leading)
        {
            if (leading == null || leading.Length == 0) return false;

            var first = leading[0];
            if ((first & 0x80) == 0) return false;

            var tag = (first & 0x40) != 0 ? first & 0x3F : (first >> 2) & 0x0F;

            // The tags a message can legitimately begin with: a public-key or passphrase session
            // key, a marker, or - for an unencrypted message - a compressed or literal packet.
            return tag == 1 || tag == 3 || tag == 8 || tag == 10 || tag == 11 || tag == 18;
        }

        // BOUNCYCASTLE'S itCount PARAMETER IS THE CODED OCTET, NOT A COUNT, and nothing in its own
        // documentation says so - it is named "itCount", takes an int, and throws
        // ArgumentOutOfRangeException("must be in range 0-255") for any honest number of anything.
        // What OpenPGP actually stores is one byte, decoded by the formula below, so the work factor
        // is not a free number at all: it is one of 256 rungs.
        //
        // Everything above this layer therefore speaks in BYTES, which is the unit S2K work is
        // genuinely measured in, and the rounding happens here - upwards, to the first rung that
        // meets what was asked for, so a caller never silently gets less stretching than it
        // requested.

        /// <summary> Decodes an S2K count octet into the number of bytes hashed. </summary>
        public static long FromCodedS2K(int coded) => (16L + (coded & 15)) << ((coded >> 4) + 6);

        /// <summary> The S2K count octet whose work is at least the requested number of bytes. </summary>
        public static int ToCodedS2K(long workBytes)
        {
            for (var coded = 0; coded <= byte.MaxValue; coded++)
                if (FromCodedS2K(coded) >= workBytes)
                    return coded;

            return byte.MaxValue;
        }

        /// <summary> Writes an encrypted message whose plaintext the callback produces. A callback
        /// rather than a stream or a byte[] because the largest thing this encrypts is a whole
        /// tar.gz being packed as it goes - buffering it first would mean a copy of the level in
        /// memory purely to satisfy a signature. </summary>
        public static async Task EncryptAsync(Func<Stream, Task> writePlaintext, Stream destination,
            char[] passphrase, PgpEncryptOptions options = null, CancellationToken token = default)
        {
            if (writePlaintext == null) throw new ArgumentNullException(nameof(writePlaintext));
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (passphrase == null || passphrase.Length == 0)
                throw new ArgumentException("A passphrase is required.", nameof(passphrase));

            options = options ?? PgpEncryptOptions.Default;
            token.ThrowIfCancellationRequested();

            var encryptor = new PgpEncryptedDataGenerator(
                SymmetricKeyAlgorithmTag.Aes256, withIntegrityPacket: true, new SecureRandom());
            encryptor.AddMethodUtf8(passphrase, HashAlgorithmTag.Sha256, ToCodedS2K(options.S2KWorkBytes));

            var name = options.InnerFileName ?? string.Empty;

            using (var encrypted = encryptor.Open(destination, new byte[CopyBufferSize]))
            {
                // An Uncompressed message skips the compressed packet entirely rather than writing
                // one that compresses nothing - which is what gpg does under --compress-algo 0, and
                // one packet fewer for a reader to walk.
                PgpCompressedDataGenerator compressor = null;
                Stream compressed = null;

                try
                {
                    var target = encrypted;
                    if (options.Compression != CompressionAlgorithmTag.Uncompressed)
                    {
                        compressor = new PgpCompressedDataGenerator(options.Compression);
                        compressed = compressor.Open(encrypted);
                        target = compressed;
                    }

                    var literal = new PgpLiteralDataGenerator();
                    using (var content = options.PlaintextLength >= 0
                               ? literal.Open(target, PgpLiteralData.Binary, name,
                                   options.PlaintextLength, PgpEncryptOptions.PinnedModTime)
                               : literal.Open(target, PgpLiteralData.Binary, name,
                                   PgpEncryptOptions.PinnedModTime, new byte[CopyBufferSize]))
                    {
                        await writePlaintext(content);
                    }
                }
                finally
                {
                    compressed?.Dispose();
                }
            }
        }

        /// <summary> Writes an encrypted message around bytes the caller already holds. </summary>
        public static Task EncryptBytesAsync(byte[] plaintext, Stream destination, char[] passphrase,
            PgpEncryptOptions options = null, CancellationToken token = default)
        {
            if (plaintext == null) throw new ArgumentNullException(nameof(plaintext));

            options = options ?? PgpEncryptOptions.Default;

            // The length is known, so the message is written with definite-length packets - the
            // shape every reader handles, including the oldest ones.
            var sized = new PgpEncryptOptions
            {
                InnerFileName = options.InnerFileName,
                Compression = options.Compression,
                S2KWorkBytes = options.S2KWorkBytes,
                PlaintextLength = plaintext.Length,
            };

            return EncryptAsync(
                stream => stream.WriteAsync(plaintext, 0, plaintext.Length, token),
                destination, passphrase, sized, token);
        }

        /// <summary> Opens an encrypted message onto the destination stream, saying why rather than
        /// throwing when it cannot: which of the failures happened is what the player is told, and
        /// none of them is exceptional. A limit being exceeded still throws - that is a hostile
        /// file, not an outcome. </summary>
        public static async Task<PgpOpenOutcome> TryDecryptAsync(Stream source, Stream destination,
            char[] passphrase, long decompressionLimit = DefaultDecompressionLimit,
            CancellationToken token = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (passphrase == null) throw new ArgumentNullException(nameof(passphrase));

            token.ThrowIfCancellationRequested();

            PgpEncryptedDataList encryptedList;
            try
            {
                var factory = new PgpObjectFactory(source);
                factory.SetThrowForUnknownCriticalPackets(true);

                // A marker packet, and anything else harmless, is walked past - the message proper
                // starts at the first session-key list.
                var pgpObject = factory.NextPgpObject();
                while (pgpObject != null && !(pgpObject is PgpEncryptedDataList))
                    pgpObject = factory.NextPgpObject();

                encryptedList = pgpObject as PgpEncryptedDataList;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                // Nothing has been decrypted yet, so a failure this early is about what the file
                // IS, never about the passphrase.
                return PgpOpenOutcome.Failed(PgpOpenResult.NotOpenPgp);
            }

            if (encryptedList == null || encryptedList.Count == 0)
                return PgpOpenOutcome.Failed(PgpOpenResult.NotOpenPgp);

            PgpPbeEncryptedData passphraseData = null;
            for (var i = 0; i < encryptedList.Count; i++)
            {
                if (!(encryptedList[i] is PgpPbeEncryptedData candidate)) continue;

                passphraseData = candidate;
                break;
            }

            // A message addressed to a key rather than to a passphrase is a real OpenPGP file this
            // reader has no key for, which is a different sentence from "wrong password".
            if (passphraseData == null)
                return PgpOpenOutcome.Failed(PgpOpenResult.Unsupported);

            // Refused rather than opened unauthenticated. gpg has written an MDC by default since
            // 2003, so a symmetric message without one is either two decades old or stripped of the
            // very thing that would report the stripping - and opening it would mean handing a
            // level back with no way to say whether it is the level that was sent.
            if (!passphraseData.IsIntegrityProtected())
                return PgpOpenOutcome.Failed(PgpOpenResult.Unsupported);

            Stream clear;
            try
            {
                clear = passphraseData.GetDataStreamUtf8(passphrase);
            }
            catch (PgpDataValidationException)
            {
                return PgpOpenOutcome.Failed(PgpOpenResult.WrongPassphrase);
            }
            catch (PgpException)
            {
                return PgpOpenOutcome.Failed(PgpOpenResult.Unsupported);
            }

            string innerFileName;
            try
            {
                var factory = new PgpObjectFactory(clear);
                factory.SetThrowForUnknownCriticalPackets(true);

                var pgpObject = factory.NextPgpObject();
                if (pgpObject is PgpCompressedData compressed)
                {
                    factory = new PgpObjectFactory(compressed.GetDataStream(decompressionLimit));
                    factory.SetThrowForUnknownCriticalPackets(true);
                    pgpObject = factory.NextPgpObject();
                }

                if (!(pgpObject is PgpLiteralData literal))
                    return PgpOpenOutcome.Failed(PgpOpenResult.Unsupported);

                innerFileName = literal.FileName;

                using (var content = literal.GetInputStream())
                    await content.CopyToAsync(destination, CopyBufferSize, token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (StreamOverflowException e)
            {
                throw new InvalidDataException(
                    $"Message decompresses to more than the {decompressionLimit} bytes allowed.", e);
            }
            catch (Exception)
            {
                // Past the session key, so the passphrase was right and what failed is the content:
                // truncated, altered, or not what the packet headers claim.
                return PgpOpenOutcome.Failed(PgpOpenResult.Tampered);
            }

            try
            {
                if (!passphraseData.Verify())
                    return PgpOpenOutcome.Failed(PgpOpenResult.Tampered);
            }
            catch (Exception)
            {
                return PgpOpenOutcome.Failed(PgpOpenResult.Tampered);
            }

            return PgpOpenOutcome.Opened(innerFileName);
        }
    }
}