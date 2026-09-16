using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BH.SDK.Services.Archive;
using BH.SDK.Services.Content;
using NUnit.Framework;

namespace BH.SDK.Tests.Services
{
    // A MIRROR OF TarGzServiceTests, CASE FOR CASE, and being able to hold the two side by side is
    // what proves the second container refuses what the first one refuses. Where a case has no twin
    // it says so in its own comment - and there are three, all of them zip facts tar has no shape
    // for: a declared size that lies, two entries under one name, and an encrypted entry.
    //
    // THE HOSTILE ARCHIVES ARE WRITTEN FIELD BY FIELD, by RawZip - see its header for why that is
    // the only way this assembly can write a zip at all. Everything here goes through ZipService or
    // through bytes.

    /// <summary> The zip half of the archive layer - the four unpack checks, the limits, AES, and
    /// the central directory that makes reading one entry cheap. </summary>
    public class ZipServiceTests
    {
        private const short BZip2 = 12;

        private static MemoryContentStore CreateSourceStore()
        {
            var store = new MemoryContentStore("source");
            store.Write("metadata.json", Encoding.UTF8.GetBytes("{\"name\":\"a level\"}"));
            store.Write("level.json", Encoding.UTF8.GetBytes("{\"objects\":[]}"));
            store.Write("audio/song.ogg", Encoding.UTF8.GetBytes("not really a song"));
            return store;
        }

        private static IReadOnlyList<ArchiveEntrySource> Entries(IContentStore store, params string[] paths)
        {
            var entries = new List<ArchiveEntrySource>(paths.Length);
            foreach (var path in paths) entries.Add(ArchiveEntrySource.FromStore(path, store));
            return entries;
        }

        private static async Task<byte[]> Pack(IReadOnlyList<ArchiveEntrySource> entries,
            ArchivePolicy policy = null)
        {
            using var buffer = new MemoryStream();
            await ZipService.PackAsync(entries, buffer, policy ?? ArchivePolicy.Default, CancellationToken.None);
            return buffer.ToArray();
        }

        private static async Task<byte[]> PackEncrypted(IReadOnlyList<ArchiveEntrySource> entries,
            string passphrase)
        {
            using var buffer = new MemoryStream();
            await ZipService.PackEncryptedAsync(entries, buffer, passphrase.ToCharArray(),
                ArchivePolicy.Default, CancellationToken.None);
            return buffer.ToArray();
        }

        private static async Task<IReadOnlyList<string>> Unpack(byte[] archive, IContentStore into,
            ArchiveLimits limits = null, string passphrase = null)
        {
            using var input = new MemoryStream(archive);
            return await ZipService.UnpackAsync(input, into, limits ?? ArchiveLimits.Default,
                passphrase?.ToCharArray(), CancellationToken.None);
        }

        private static async Task<string> ReadText(IContentStore store, string path)
        {
            using var stream = await store.OpenReadAsync(path, CancellationToken.None);
            using var reader = new StreamReader(stream, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }

        // Reads the DOS date out of the first local header, which is where the pinned time has to
        // land for two packs of the same content to be the same bytes.
        private static int DosDateOfFirstEntry(byte[] archive) =>
            archive[12] | (archive[13] << 8);

        // The AE-2 header of the first entry: a local header is 30 bytes plus the name plus the
        // extra field, and the entry data then opens with the salt and the two-byte verifier. 16
        // bytes of salt is what AES-256 takes - ZipService pins the key size, so this does too.
        private static (byte[] Salt, int Verifier) AesHeaderOfFirstEntry(byte[] archive)
        {
            var nameLength = archive[26] | (archive[27] << 8);
            var extraLength = archive[28] | (archive[29] << 8);
            var data = 30 + nameLength + extraLength;

            var salt = new byte[16];
            Array.Copy(archive, data, salt, 0, salt.Length);

            return (salt, archive[data + 16] | (archive[data + 17] << 8));
        }

        // Two keys and a verifier, in that order, out of one PBKDF2 stream - the derivation AE-2
        // specifies. Only the last two bytes are read here; the keys themselves are what SharpZipLib
        // uses to do the actual work.
        private static int VerifierOf(byte[] passphrase, byte[] salt)
        {
            using var derivation = new Rfc2898DeriveBytes(passphrase, salt, 1000, HashAlgorithmName.SHA1);
            var material = derivation.GetBytes(32 + 32 + 2);

            return material[64] | (material[65] << 8);
        }

        // What a single-byte code page does to a string it cannot represent - the shape the defect
        // above took. Written by hand because netstandard2.1 carries no legacy code page at all.
        private static byte[] SingleByteBytes(string passphrase)
        {
            var bytes = new byte[passphrase.Length];
            for (var i = 0; i < passphrase.Length; i++) bytes[i] = (byte)passphrase[i];

            return bytes;
        }

        #region Round trip

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Pack_ThenUnpack_RoundTrips()
        {
            var source = CreateSourceStore();
            var packed = await Pack(Entries(source, "metadata.json", "level.json", "audio/song.ogg"));

            var destination = new MemoryContentStore("destination");
            var written = await Unpack(packed, destination);

            Assert.AreEqual(new[] { "metadata.json", "level.json", "audio/song.ogg" }, written);
            Assert.AreEqual("{\"name\":\"a level\"}", await ReadText(destination, "metadata.json"));
            Assert.AreEqual("{\"objects\":[]}", await ReadText(destination, "level.json"));
            Assert.AreEqual("not really a song", await ReadText(destination, "audio/song.ogg"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public async Task Pack_WritesSomethingTheSnifferCallsAZip()
        {
            var source = CreateSourceStore();
            var packed = await Pack(Entries(source, "level.json"));

            Assert.AreEqual(ArchiveFormat.Zip, ArchiveFormatSniffer.Detect(packed));
        }

        // The claim ArchivePolicy makes, and the one NonSeekableWrite exists to keep: a zip written
        // to a file and a zip written to a pipe have to be the same archive, or a backend cannot
        // recognise a re-upload by its digest. ZipOutputStream patches the local header when it can
        // seek and writes a data descriptor when it cannot, so without the wrapper this fails on
        // one platform and passes on another.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Pack_Twice_ProducesIdenticalBytes()
        {
            var source = CreateSourceStore();

            var first = await Pack(Entries(source, "metadata.json", "level.json", "audio/song.ogg"));
            var second = await Pack(Entries(source, "metadata.json", "level.json", "audio/song.ogg"));

            Assert.AreEqual(first, second);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task Pack_StampsThePinnedTimeRatherThanTheClock()
        {
            var source = CreateSourceStore();
            var packed = await Pack(Entries(source, "level.json"));

            // 1980-01-01 in DOS date form: year 0 since 1980, month 1, day 1.
            Assert.AreEqual(0x0021, DosDateOfFirstEntry(packed));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task Pack_RefusesANameLongerThanTheSharedBudget()
        {
            var source = new MemoryContentStore("source");
            var longName = new string('z', ArchivePolicy.MaxEntryNameBytes) + ".json";
            source.Write(longName, Encoding.UTF8.GetBytes("{}"));

            var entries = Entries(source, longName);

            await AsyncAssert.Throws<InvalidDataException>(async () =>
            {
                using var buffer = new MemoryStream();
                await ZipService.PackAsync(entries, buffer, ArchivePolicy.Default, CancellationToken.None);
            });
        }

        #endregion

        #region The four checks

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task Unpack_RefusesATraversalName()
        {
            var archive = RawZip.Build(new RawZip.Entry("../escaped.json", "{}"));
            var destination = new MemoryContentStore("destination");

            await AsyncAssert.Throws<InvalidDataException>(async () => await Unpack(archive, destination));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task Unpack_RefusesABackslashSeparator()
        {
            var archive = RawZip.Build(new RawZip.Entry("nested\\file.json", "{}"));
            var destination = new MemoryContentStore("destination");

            await AsyncAssert.Throws<InvalidDataException>(async () => await Unpack(archive, destination));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task Unpack_RefusesARootedName()
        {
            var archive = RawZip.Build(new RawZip.Entry("/tmp/level.json", "{}"));
            var destination = new MemoryContentStore("destination");

            await AsyncAssert.Throws<InvalidDataException>(async () => await Unpack(archive, destination));
        }

        // NO TAR TWIN. In a tar two entries under one name degrade into a silent overwrite; here it
        // is the classic zip confusion - whatever inspected the archive saw the first entry,
        // whatever extracted it wrote the second - so it is refused outright.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Unpack_RefusesTwoEntriesSharingAName()
        {
            var archive = RawZip.Build(
                new RawZip.Entry("level.json", "{\"first\":1}"),
                new RawZip.Entry("level.json", "{\"second\":2}"));

            var destination = new MemoryContentStore("destination");

            await AsyncAssert.Throws<InvalidDataException>(async () => await Unpack(archive, destination));
        }

        // The zip spelling of tar's link entry: the kind lives in the unix mode inside
        // ExternalFileAttributes, and a symlink pointing outward is a traversal whose NAME is
        // innocent, which is why no name check catches it. 0xA1FF is S_IFLNK plus 0777.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Unpack_RefusesASymlinkEntry()
        {
            var archive = RawZip.Build(new RawZip.Entry("logo.png", "/etc/passwd", RawZip.Stored,
                unchecked((int)0xA1FF0000)));

            var destination = new MemoryContentStore("destination");

            await AsyncAssert.Throws<InvalidDataException>(async () => await Unpack(archive, destination));
        }

        // A zip made by Windows Explorer records no unix mode at all, and refusing that would
        // refuse the most ordinary archive there is.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task Unpack_AcceptsAnEntryWithNoUnixMode()
        {
            var archive = RawZip.Build(new RawZip.Entry("level.json", "{\"objects\":[]}"));
            var destination = new MemoryContentStore("destination");

            var written = await Unpack(archive, destination);

            Assert.AreEqual(new[] { "level.json" }, written);
            Assert.AreEqual("{\"objects\":[]}", await ReadText(destination, "level.json"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task Unpack_SkipsADirectoryEntry()
        {
            var archive = RawZip.Build(
                new RawZip.Entry("audio/", string.Empty, RawZip.Stored, 0x41FF0000),
                new RawZip.Entry("audio/song.ogg", "not a song"));

            var destination = new MemoryContentStore("destination");
            var written = await Unpack(archive, destination);

            Assert.AreEqual(new[] { "audio/song.ogg" }, written);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task Unpack_RefusesAnExoticCompressionMethod()
        {
            var archive = RawZip.Build(new RawZip.Entry("level.json", "{}", BZip2));
            var destination = new MemoryContentStore("destination");

            await AsyncAssert.Throws<InvalidDataException>(async () => await Unpack(archive, destination));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Unpack_RefusesMoreEntriesThanAllowed()
        {
            var source = new MemoryContentStore("source");
            var paths = new List<string>();
            for (var i = 0; i < 6; i++)
            {
                var path = $"file{i}.json";
                source.Write(path, Encoding.UTF8.GetBytes("{}"));
                paths.Add(path);
            }

            var packed = await Pack(Entries(source, paths.ToArray()));
            var destination = new MemoryContentStore("destination");

            await AsyncAssert.Throws<InvalidDataException>(async () =>
                await Unpack(packed, destination, new ArchiveLimits { MaxEntries = 3 }));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Unpack_RefusesAnEntryOverTheEntryCap()
        {
            var source = new MemoryContentStore("source");
            source.Write("big.bin", new byte[4096]);

            var packed = await Pack(Entries(source, "big.bin"));
            var destination = new MemoryContentStore("destination");

            await AsyncAssert.Throws<InvalidDataException>(async () =>
                await Unpack(packed, destination, new ArchiveLimits { MaxEntryBytes = 1024 }));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Unpack_RefusesMoreTotalBytesThanAllowed()
        {
            var source = new MemoryContentStore("source");
            source.Write("one.bin", new byte[2048]);
            source.Write("two.bin", new byte[2048]);

            var packed = await Pack(Entries(source, "one.bin", "two.bin"));
            var destination = new MemoryContentStore("destination");

            await AsyncAssert.Throws<InvalidDataException>(async () =>
                await Unpack(packed, destination, new ArchiveLimits { MaxTotalBytes = 3000 }));
        }

        // NO TAR TWIN, and the reason the limit is enforced twice. In a tar the header length IS
        // the payload length; in a zip the directory is a claim the archive makes about itself, so
        // an entry can declare a kilobyte and inflate into as much as the reader will hold. Here
        // the directory declares 32 bytes and the entry really holds a quarter of a megabyte: the
        // cheap check waves it through, and the byte counter during the copy is what stops it.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task Unpack_RefusesAnEntryThatLiesAboutItsSize()
        {
            var archive = RawZip.Build(new RawZip.Entry("bomb.bin", new string('a', 256 * 1024)));
            Lie(archive, declaredSize: 32);

            var destination = new MemoryContentStore("destination");

            await AsyncAssert.Throws<InvalidDataException>(async () =>
                await Unpack(archive, destination, new ArchiveLimits { MaxTotalBytes = 64 * 1024 }));
        }

        // Rewrites both copies of the uncompressed size - the local header's and the central
        // directory's - leaving the payload as it is. That is what a bomb does.
        private static void Lie(byte[] archive, int declaredSize)
        {
            for (var i = 0; i + 4 <= archive.Length; i++)
            {
                var signature = BitConverter.ToInt32(archive, i);
                var sizeOffset = signature == 0x04034b50 ? i + 22 : signature == 0x02014b50 ? i + 24 : -1;
                if (sizeOffset < 0) continue;

                var value = BitConverter.GetBytes(declaredSize);
                Array.Copy(value, 0, archive, sizeOffset, 4);
            }
        }

        #endregion

        #region Encryption

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task PackEncrypted_ThenUnpackWithThePassphrase_RoundTrips()
        {
            var source = CreateSourceStore();

            // Non-ASCII on purpose: the audience is Russian-speaking, and a passphrase that only
            // works in ASCII is a default failure rather than an edge case.
            var archive = await PackEncrypted(Entries(source, "level.json", "audio/song.ogg"),
                "пароль-Ünïcode");

            var destination = new MemoryContentStore("destination");
            var written = await Unpack(archive, destination, ArchiveLimits.Default, "пароль-Ünïcode");

            Assert.AreEqual(new[] { "level.json", "audio/song.ogg" }, written);
            Assert.AreEqual("{\"objects\":[]}", await ReadText(destination, "level.json"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task Unpack_WithoutThePassphrase_AsksForOne()
        {
            var source = CreateSourceStore();
            var archive = await PackEncrypted(Entries(source, "level.json"), "secret");
            var destination = new MemoryContentStore("destination");

            await AsyncAssert.Throws<ArchivePassphraseRequiredException>(async () =>
                await Unpack(archive, destination));
        }

        // The other half of the same split: "not given" is a question to ask, "wrong" is an answer
        // already given, and telling a player they got a password wrong before they typed one is
        // the defect the two exceptions exist to prevent.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task Unpack_WithTheWrongPassphrase_SaysSo()
        {
            var source = CreateSourceStore();
            var archive = await PackEncrypted(Entries(source, "level.json"), "secret");
            var destination = new MemoryContentStore("destination");

            await AsyncAssert.Throws<ArchiveWrongPassphraseException>(async () =>
                await Unpack(archive, destination, ArchiveLimits.Default, "wrong"));
        }

        // THE DEFECT THIS TEST EXISTS FOR SHIPPED ONCE AND WAS INVISIBLE FROM INSIDE. SharpZipLib
        // encodes a password with the machine's ANSI code page, AE-2 derives the key from its UTF-8
        // bytes, and a pack/unpack pair that shares the mistake round trips perfectly - so the
        // archive opened here and nowhere else, which is the worst shape a bug can take in a format
        // whose whole point is that other tools read it.
        //
        // So the verifier is recomputed from the spec instead of from the library: PBKDF2-HMAC-SHA1
        // over the passphrase bytes and the salt the archive carries, 1000 iterations, two keys and
        // then the two-byte password verification value. It matches the UTF-8 reading of the
        // passphrase and no other, which is the interoperability claim itself rather than a proxy
        // for it. An ASCII passphrase would pass under either reading and prove nothing.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task PackEncrypted_DerivesTheKeyFromTheUtf8Passphrase()
        {
            const string passphrase = "пароль-Ünïcode";

            var source = CreateSourceStore();
            var archive = await PackEncrypted(Entries(source, "level.json"), passphrase);
            var (salt, verifier) = AesHeaderOfFirstEntry(archive);

            Assert.AreEqual(verifier, VerifierOf(Encoding.UTF8.GetBytes(passphrase), salt),
                "AE-2 derives the key from the UTF-8 bytes of the passphrase.");
            Assert.AreNotEqual(verifier, VerifierOf(SingleByteBytes(passphrase), salt),
                "A single-byte reading of the passphrase must not be what was used.");
        }

        // The price of AE-2, pinned rather than regretted: the entry NAMES stay in the clear, so an
        // archiver lists an encrypted level and a passphrase covers the content alone. An import
        // preview reads this list before it asks for anything, which is what makes the question
        // "this level needs a password" rather than "this file is unreadable".
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task PackEncrypted_LeavesTheNamesReadable()
        {
            var source = CreateSourceStore();
            var archive = await PackEncrypted(Entries(source, "metadata.json", "level.json"), "secret");

            using var input = new MemoryStream(archive);
            var names = ZipService.List(input, out var encrypted, ArchiveLimits.Default);

            Assert.IsTrue(encrypted);
            CollectionAssert.AreEquivalent(new[] { "metadata.json", "level.json" }, names);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task List_SaysWhenAPassphraseWillBeNeeded()
        {
            var source = CreateSourceStore();
            var archive = await PackEncrypted(Entries(source, "level.json"), "secret");

            using var input = new MemoryStream(archive);
            ZipService.List(input, out var encrypted, ArchiveLimits.Default);

            Assert.IsTrue(encrypted);
        }

        #endregion

        #region The central directory

        // What tar.gz structurally cannot do. The entry is last in the archive and is read without
        // decompressing what comes before it.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task ReadEntries_TakesAnEntryFromAnywhereInTheArchive()
        {
            var source = CreateSourceStore();
            var packed = await Pack(Entries(source, "audio/song.ogg", "level.json", "metadata.json"));

            using var input = new MemoryStream(packed);
            var found = await ZipService.ReadEntriesAsync(input, new[] { "metadata.json" },
                ArchiveLimits.Default, null, CancellationToken.None);

            Assert.AreEqual(1, found.Count);
            Assert.AreEqual("{\"name\":\"a level\"}", Encoding.UTF8.GetString(found["metadata.json"]));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task ReadEntries_SkipsWhatTheArchiveDoesNotHold()
        {
            var source = CreateSourceStore();
            var packed = await Pack(Entries(source, "level.json"));

            using var input = new MemoryStream(packed);
            var found = await ZipService.ReadEntriesAsync(input, new[] { "metadata.json", "level.json" },
                ArchiveLimits.Default, null, CancellationToken.None);

            Assert.AreEqual(new[] { "level.json" }, new List<string>(found.Keys));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task List_NamesEveryEntryWithoutUnpackingAnything()
        {
            var source = CreateSourceStore();
            var packed = await Pack(Entries(source, "level.json", "metadata.json"));

            using var input = new MemoryStream(packed);
            var names = ZipService.List(input, out var encrypted, ArchiveLimits.Default);

            Assert.AreEqual(new[] { "level.json", "metadata.json" }, names);
            Assert.IsFalse(encrypted);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task Unpack_RefusesSomethingThatOnlyStartsLikeAZip()
        {
            var destination = new MemoryContentStore("destination");
            var rubbish = Encoding.UTF8.GetBytes("PK and then nothing that parses");

            await AsyncAssert.Throws<InvalidDataException>(async () => await Unpack(rubbish, destination));
        }

        #endregion
    }
}