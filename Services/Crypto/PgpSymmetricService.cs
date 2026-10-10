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
    // Exactly what `gpg --symmetric --cipher-algo AES256` produces: SKESK v4, S2K 3 / SHA-256, SEIPD v1
    // (not RFC 9580: GnuPG does not implement it), AES-256, literal data. The Utf8 overloads are
    // MANDATORY (gpg reads a passphrase as UTF-8). Verify() is always called. Docs/ARCHIVE.md.

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

        // BouncyCastle's itCount is the CODED OCTET (0-255), not a count. Callers speak in BYTES hashed;
        // the rounding is here, upwards, so a caller never gets less stretching than it asked for.

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

            // Refused rather than opened unauthenticated: gpg has written an MDC by default since 2003.
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