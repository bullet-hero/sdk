using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BH.SDK.Services.Crypto;
using NUnit.Framework;

namespace BH.SDK.Tests.Services
{
    // THE CYRILLIC PASSPHRASE IS THE LOAD-BEARING TEST HERE, and it looks like the least important
    // one. BouncyCastle offers two spellings of every passphrase call: the plain one encodes a
    // passphrase byte-per-char, the ...Utf8 one encodes it the way gpg does. For an ASCII passphrase
    // the two agree exactly, so every test written in English passes under either - and a level
    // exported by a Russian-speaking author then fails to open in gpg, silently, reported as a wrong
    // password. Since that is the DEFAULT case for this project's audience, the round trip below is
    // what stops the wrong overload from ever being used.
    //
    // The other thing being pinned is that a wrong passphrase and a damaged file are TWO ANSWERS.
    // Without the MDC there is only one - both arrive as the decryption failing - and the player is
    // told something useless about two situations with completely different remedies.
    //
    // S2KWorkBytes is turned down to its floor throughout: what is under test is the format, and the
    // work factor is a parameter of it. One test runs at the shipped default so that number is not
    // untested either.
    public class PgpSymmetricServiceTests
    {
        private const int FastS2K = 1024;

        private static readonly char[] AsciiPassphrase = "correct horse".ToCharArray();
        private static readonly char[] CyrillicPassphrase = "пароль уровня".ToCharArray();

        private static byte[] Plaintext => Encoding.UTF8.GetBytes("{\"objects\":[],\"name\":\"уровень\"}");

        private static PgpEncryptOptions Document(string innerFileName = "level.json",
            int workBytes = FastS2K)
        {
            var options = PgpEncryptOptions.ForDocument(innerFileName);
            options.S2KWorkBytes = workBytes;
            return options;
        }

        private static PgpEncryptOptions Archive(string innerFileName)
        {
            var options = PgpEncryptOptions.ForArchive(innerFileName);
            options.S2KWorkBytes = FastS2K;
            return options;
        }

        private static async Task<byte[]> Encrypt(byte[] plaintext, char[] passphrase,
            PgpEncryptOptions options = null)
        {
            using var buffer = new MemoryStream();
            await PgpSymmetricService.EncryptBytesAsync(plaintext, buffer, passphrase,
                options ?? Document(), CancellationToken.None);
            return buffer.ToArray();
        }

        private static async Task<(PgpOpenOutcome Outcome, byte[] Plaintext)> Decrypt(byte[] message,
            char[] passphrase)
        {
            using var source = new MemoryStream(message);
            using var destination = new MemoryStream();

            var outcome = await PgpSymmetricService.TryDecryptAsync(source, destination, passphrase,
                PgpSymmetricService.DefaultDecompressionLimit, CancellationToken.None);

            return (outcome, destination.ToArray());
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task Encrypt_ThenDecrypt_RoundTrips()
        {
            var message = await Encrypt(Plaintext, AsciiPassphrase);
            var (outcome, plaintext) = await Decrypt(message, AsciiPassphrase);

            Assert.AreEqual(PgpOpenResult.Ok, outcome.Result);
            Assert.AreEqual(Plaintext, plaintext);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task Encrypt_ThenDecrypt_RoundTripsANonAsciiPassphrase()
        {
            var message = await Encrypt(Plaintext, CyrillicPassphrase);
            var (outcome, plaintext) = await Decrypt(message, CyrillicPassphrase);

            Assert.AreEqual(PgpOpenResult.Ok, outcome.Result);
            Assert.AreEqual(Plaintext, plaintext);
        }

        // Reported, not thrown: which of the failures happened is a sentence shown to a player, and
        // none of them is exceptional.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task Decrypt_WithTheWrongPassphrase_SaysSo()
        {
            var message = await Encrypt(Plaintext, AsciiPassphrase);
            var (outcome, _) = await Decrypt(message, "not the passphrase".ToCharArray());

            Assert.AreEqual(PgpOpenResult.WrongPassphrase, outcome.Result);
        }

        // One byte, in the middle - inside the ciphertext rather than in a header - so what catches
        // it is the MDC and nothing else. This is the case that would be indistinguishable from a
        // wrong passphrase without one.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task Decrypt_AnAlteredMessage_SaysItIsDamaged()
        {
            var message = await Encrypt(Plaintext, AsciiPassphrase);
            message[message.Length - 8] ^= 0xFF;

            var (outcome, _) = await Decrypt(message, AsciiPassphrase);

            Assert.AreEqual(PgpOpenResult.Tampered, outcome.Result);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task Decrypt_SomethingThatIsNotOpenPgp_SaysSo()
        {
            var (outcome, _) = await Decrypt(Encoding.UTF8.GetBytes("{\"objects\":[]}"), AsciiPassphrase);

            Assert.AreEqual(PgpOpenResult.NotOpenPgp, outcome.Result);
        }

        // The inner name is how a package tells a level.json from a level.bson without inventing a
        // header of its own - it is the same thing gpg restores the file as.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task Decrypt_ReportsTheInnerFileName()
        {
            var message = await Encrypt(Plaintext, AsciiPassphrase, Document("level.bson"));
            var (outcome, _) = await Decrypt(message, AsciiPassphrase);

            Assert.AreEqual(PgpOpenResult.Ok, outcome.Result);
            Assert.AreEqual("level.bson", outcome.InnerFileName);
        }

        // An already-compressed payload - a .tar.gz - skips the compressed packet entirely, which is
        // what gpg does under --compress-algo 0 and one packet fewer for a reader to walk.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task Encrypt_RoundTripsWithoutCompression()
        {
            var message = await Encrypt(Plaintext, AsciiPassphrase,
                Archive("level.tar.gz"));

            var (outcome, plaintext) = await Decrypt(message, AsciiPassphrase);

            Assert.AreEqual(PgpOpenResult.Ok, outcome.Result);
            Assert.AreEqual(Plaintext, plaintext);
        }

        // The streaming shape, which is what a .tar.gz.gpg actually uses: the archive is packed
        // straight into the message, so its length is not known when the literal packet is opened.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task Encrypt_RoundTripsWhenTheLengthIsNotKnownUpFront()
        {
            var plaintext = Plaintext;

            using var buffer = new MemoryStream();
            await PgpSymmetricService.EncryptAsync(
                stream => stream.WriteAsync(plaintext, 0, plaintext.Length, CancellationToken.None),
                buffer, AsciiPassphrase, Document(), CancellationToken.None);

            var (outcome, decrypted) = await Decrypt(buffer.ToArray(), AsciiPassphrase);

            Assert.AreEqual(PgpOpenResult.Ok, outcome.Result);
            Assert.AreEqual(plaintext, decrypted);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task LooksLikeOpenPgp_RecognisesWhatThisWrites()
        {
            var message = await Encrypt(Plaintext, AsciiPassphrase);

            Assert.IsTrue(PgpSymmetricService.LooksLikeOpenPgp(message));
            Assert.IsFalse(PgpSymmetricService.LooksLikeOpenPgp(Encoding.UTF8.GetBytes("{\"a\":1}")));
            Assert.IsFalse(PgpSymmetricService.LooksLikeOpenPgp(new byte[] { 0x1f, 0x8b }));
        }

        // The shipped work factor, run once. Everything else here turns it down to keep the suite
        // quick, which would otherwise leave the number nobody sees until a phone runs it untested.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task Encrypt_RoundTripsAtTheShippedWorkFactor()
        {
            var message = await Encrypt(Plaintext, CyrillicPassphrase,
                Document(workBytes: PgpEncryptOptions.DefaultS2KWorkBytes));

            var (outcome, plaintext) = await Decrypt(message, CyrillicPassphrase);

            Assert.AreEqual(PgpOpenResult.Ok, outcome.Result);
            Assert.AreEqual(Plaintext, plaintext);
        }

        // The conversion that made every test above fail once. BouncyCastle's itCount parameter is
        // the coded octet OpenPGP stores, not a number of anything, and it throws for any honest
        // value - so the work factor is one of 256 rungs and this is where a request in bytes lands
        // on one. Rounding UP is the direction that matters: a caller must never silently get less
        // stretching than it asked for.
        [TestCase(1, ExpectedResult = 1024)]
        [TestCase(1024, ExpectedResult = 1024)]
        [TestCase(1025, ExpectedResult = 1088)]
        [TestCase(16 * 1024 * 1024, ExpectedResult = 16 * 1024 * 1024)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public long ToCodedS2K_RoundsUpToARung(long workBytes) =>
            PgpSymmetricService.FromCodedS2K(PgpSymmetricService.ToCodedS2K(workBytes));

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ToCodedS2K_StaysInsideTheOctet()
        {
            Assert.AreEqual(0, PgpSymmetricService.ToCodedS2K(0));
            Assert.AreEqual(byte.MaxValue, PgpSymmetricService.ToCodedS2K(long.MaxValue));

            // Monotone, which is what makes a linear scan for the first big-enough rung correct.
            for (var coded = 1; coded <= byte.MaxValue; coded++)
                Assert.Greater(PgpSymmetricService.FromCodedS2K(coded),
                    PgpSymmetricService.FromCodedS2K(coded - 1));
        }
    }
}