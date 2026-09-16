using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BH.SDK.Models;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Resources;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Services.Archive;
using BH.SDK.Services.Content;
using BH.SDK.Services.LevelArchive;
using BH.SDK.Utils;
using NUnit.Framework;

namespace BH.SDK.Tests.Services
{
    // THE WHOLE PIPELINE, WITH NO DISK ANYWHERE - which is the server's path exactly. A backend
    // accepts an archive, hands it to the reader and gets a store back; nothing in that sentence
    // mentions a file system, and these tests are what makes it true rather than aspirational.
    //
    // The seven export modes are three independent choices - folder or file, which container, and
    // which protection scheme - and what is checked here is that they stay independent: a protected
    // ARCHIVE is opaque from the outside, while a protected FOLDER encrypts the level document alone
    // and leaves the metadata and the cover readable, so a browser can still draw the card and ask
    // for the passphrase only when the level is opened. Those are different promises, and each is
    // worth a test that would fail if the other were implemented.
    //
    // THE FORMAT AXIS IS THE NEWEST OF THE THREE and the one most likely to rot: everything above
    // the "Every container" region was written when tar.gz was the only container there was, so a
    // case that names it explicitly is testing tar.gz rather than "an archive".

    /// <summary> The whole pipeline with no disk anywhere - which is the server's path exactly - and the
    /// three independent choices behind the seven export modes. </summary>
    public class LevelArchiveRoundTripTests
    {
        private static readonly char[] Passphrase = "пароль уровня".ToCharArray();

        private static SerializationService Serialization => new SerializationService();

        private static MemoryContentStore CreateLevelStore()
        {
            var store = new MemoryContentStore("level");
            store.Write(FileNames.LogoFileNamePng, Encoding.UTF8.GetBytes("a cover"));
            store.Write("texture.png", Encoding.UTF8.GetBytes("a texture"));
            store.Write("audio/song.ogg", Encoding.UTF8.GetBytes("not really a song"));
            return store;
        }

        private static (Level Level, LevelMeta Meta) CreateLevel()
        {
            var level = MockData.CreateTestLevel();
            var meta = MockData.CreateTestLevelMeta();

            meta.LevelLogo = new ResourceKey(ResourceUriType.LevelPath, FileNames.LogoFileNamePng);
            Replace(level.Resources.Textures.Values.First(), "texture.png");
            Replace(level.Resources.Audios.Values.First(), "audio/song.ogg");

            return (level, meta);
        }

        private static void Replace(Resource resource, string levelPath)
        {
            resource.Sources.Clear();
            resource.Sources.Add(new ResourceKey(ResourceUriType.LevelPath, levelPath));
        }

        private static async Task<(LevelArchivePlan Plan, MemoryContentStore Store)> PlanAsync()
        {
            var (level, meta) = CreateLevel();
            var store = CreateLevelStore();
            var plan = await LevelArchiveBuilder.BuildAsync(level, meta, store, CancellationToken.None);
            return (plan, store);
        }

        private static async Task<byte[]> WriteArchiveAsync(LevelArchivePlan plan, IContentStore source,
            char[] passphrase = null, LevelArchiveOptions options = null)
        {
            using var buffer = new MemoryStream();
            await LevelArchiveWriter.WriteArchiveAsync(plan, source, buffer, Serialization, options,
                passphrase, CancellationToken.None);
            return buffer.ToArray();
        }

        private static Task<LevelArchiveContent> ReadArchiveAsync(byte[] archive, char[] passphrase = null)
        {
            var source = new MemoryStream(archive, writable: false);
            return LevelArchiveReader.ReadAsync(source, passphrase, token: CancellationToken.None);
        }

        // READING A LEVEL DOCUMENT IS TWO STEPS, and an archive's is a level document like any other:
        // the bytes carry a placement rather than the copies it stands for, so a reader that stops
        // at the deserializer holds a level that is missing everything every prefab contributes.
        // LevelArchiveGenerator - the real importer - does exactly this pair; so does
        // LevelLoaderService on the host side.
        private static Level Deserialize(LevelArchiveContent content)
        {
            var level = Serialization.DeserializeEnvelope<Level>(content.LevelBytes, content.LevelFormat);
            PrefabVirtualizationUtils.Expand(level);
            return level;
        }

        private static LevelMeta DeserializeMeta(LevelArchiveContent content) =>
            Serialization.DeserializeEnvelope<LevelMeta>(content.MetaBytes, content.MetaFormat);

        #region Every container

        private static LevelArchiveOptions For(ArchiveFormat format,
            ArchiveProtection protection = ArchiveProtection.None) =>
            new LevelArchiveOptions { Format = format, Protection = protection };

        // BOTH CONTAINERS, ONE PIPELINE, and the axis is what proves it. Everything above this
        // region was written when tar.gz was the only shape an archive could take; a zip that
        // round trips through the same builder, writer and reader is what makes "second first-class
        // format" a fact rather than a claim in a document.
        [Test]
        [TestCase(ArchiveFormat.TarGz)]
        [TestCase(ArchiveFormat.Zip)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task EveryFormat_RoundTrips(ArchiveFormat format)
        {
            var (plan, store) = await PlanAsync();
            var archive = await WriteArchiveAsync(plan, store, options: For(format));

            Assert.AreEqual(format, ArchiveFormatSniffer.Detect(archive));

            var content = await ReadArchiveAsync(archive);

            Assert.AreEqual(LevelArchiveOpenResult.Ok, content.Result);
            Assert.AreEqual(plan.Level, Deserialize(content));
            Assert.AreEqual(plan.Meta, DeserializeMeta(content));

            CollectionAssert.AreEquivalent(
                new[] { "audio/song.ogg", FileNames.LogoFileNamePng, "texture.png" },
                content.ResourceFileNames);
        }

        // The OpenPGP layer does not care which container it wraps, and the reader finds out by
        // sniffing the plaintext rather than by assuming. This is the case that fails the moment
        // somebody re-hardcodes gzip after the message is opened.
        [Test]
        [TestCase(ArchiveFormat.TarGz)]
        [TestCase(ArchiveFormat.Zip)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task EveryFormat_RoundTripsBehindOpenPgp(ArchiveFormat format)
        {
            var (plan, store) = await PlanAsync();
            var archive = await WriteArchiveAsync(plan, store, Passphrase,
                For(format, ArchiveProtection.OpenPgp));

            Assert.AreEqual(ArchiveFormat.OpenPgp, ArchiveFormatSniffer.Detect(archive));

            var unopened = await ReadArchiveAsync(archive);
            Assert.AreEqual(LevelArchiveOpenResult.PassphraseRequired, unopened.Result);

            var content = await ReadArchiveAsync(archive, Passphrase);

            Assert.AreEqual(LevelArchiveOpenResult.Ok, content.Result);
            Assert.AreEqual(plan.Level, Deserialize(content));
        }

        // The zip's own AES. From the outside this is an ordinary zip - the sniffer says Zip, an
        // archiver lists the names - and the passphrase question only comes up once an entry is
        // opened, which is why the reader has to ask it there rather than up front.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task ZipEncrypted_LooksLikeAZipAndStillAsksForThePassphrase()
        {
            var (plan, store) = await PlanAsync();
            var archive = await WriteArchiveAsync(plan, store, Passphrase,
                For(ArchiveFormat.Zip, ArchiveProtection.ZipAes256));

            Assert.AreEqual(ArchiveFormat.Zip, ArchiveFormatSniffer.Detect(archive));

            var unopened = await ReadArchiveAsync(archive);
            var misopened = await ReadArchiveAsync(archive, "not it".ToCharArray());

            Assert.AreEqual(LevelArchiveOpenResult.PassphraseRequired, unopened.Result);
            Assert.AreEqual(LevelArchiveOpenResult.WrongPassphrase, misopened.Result);

            var content = await ReadArchiveAsync(archive, Passphrase);

            Assert.AreEqual(LevelArchiveOpenResult.Ok, content.Result);
            Assert.AreEqual(plan.Level, Deserialize(content));
            Assert.AreEqual(plan.Meta, DeserializeMeta(content));

            CollectionAssert.AreEquivalent(
                new[] { "audio/song.ogg", FileNames.LogoFileNamePng, "texture.png" },
                content.ResourceFileNames);
        }

        // WHAT A PASSPHRASE ALONE MEANS, which is the question the export UI now answers by
        // default. The options say WHICH scheme; a caller that says nothing and hands over a
        // passphrase gets the container's own answer - AES for a zip, because the person receiving
        // the level opens it with the archiver they already have, and the OpenPGP layer for a
        // .tar.gz, which has no encryption of its own to fall back to. The one reading that is NOT
        // available is writing it in the clear, and the sniffer is what tells the two apart.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Zip_WithAPassphraseAndNoScheme_GetsTheZipsOwnAes()
        {
            var (plan, store) = await PlanAsync();
            var archive = await WriteArchiveAsync(plan, store, Passphrase, For(ArchiveFormat.Zip));

            Assert.AreEqual(ArchiveFormat.Zip, ArchiveFormatSniffer.Detect(archive));

            var unopened = await ReadArchiveAsync(archive);
            Assert.AreEqual(LevelArchiveOpenResult.PassphraseRequired, unopened.Result);

            var content = await ReadArchiveAsync(archive, Passphrase);

            Assert.AreEqual(LevelArchiveOpenResult.Ok, content.Result);
            Assert.AreEqual(plan.Level, Deserialize(content));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task TarGz_WithAPassphraseAndNoScheme_FallsBackToOpenPgp()
        {
            var (plan, store) = await PlanAsync();
            var archive = await WriteArchiveAsync(plan, store, Passphrase, For(ArchiveFormat.TarGz));

            Assert.AreEqual(ArchiveFormat.OpenPgp, ArchiveFormatSniffer.Detect(archive));

            var content = await ReadArchiveAsync(archive, Passphrase);

            Assert.AreEqual(LevelArchiveOpenResult.Ok, content.Result);
            Assert.AreEqual(plan.Level, Deserialize(content));
        }

        // THE IMPORT DIRECTION, WITH NOTHING OF OURS ON THE WRITING SIDE. Every case above packs
        // with the writer under test, so together they prove only that the pair agrees with itself;
        // a level arriving from a player was zipped by Explorer, by `zip -r` or by a website. This
        // one is built field by field - stored entries, no unix mode, no extra fields, the shape a
        // desktop produces - and goes through the reader a player's file actually goes through.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task AZipWrittenBySomebodyElse_ImportsAsALevel()
        {
            var (plan, _) = await PlanAsync();

            var archive = RawZip.Build(
                new RawZip.Entry(FileNames.MetadataFileBaseName + ".json",
                    Serialization.SerializeEnvelope(plan.Meta, SerializationType.Json)),
                new RawZip.Entry(FileNames.LevelFileBaseName + ".json",
                    Serialization.SerializeEnvelope(PrefabVirtualizationUtils.Thin(plan.Level),
                        SerializationType.Json)),
                new RawZip.Entry(FileNames.LogoFileNamePng, "a cover"),
                new RawZip.Entry("texture.png", "a texture"),
                new RawZip.Entry("audio/song.ogg", "not really a song"));

            var content = await ReadArchiveAsync(archive);

            Assert.AreEqual(LevelArchiveOpenResult.Ok, content.Result);
            Assert.AreEqual(ArchiveFormat.Zip, content.Format);
            Assert.AreEqual(plan.Level, Deserialize(content));
            Assert.AreEqual(plan.Meta, DeserializeMeta(content));

            CollectionAssert.AreEquivalent(
                new[] { "audio/song.ogg", FileNames.LogoFileNamePng, "texture.png" },
                content.ResourceFileNames);
        }

        // Refused by name rather than read as damage. A person whose 7z is called corrupt goes
        // looking for a problem that is not there; a person told this build cannot read 7z re-zips
        // the file and moves on.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task SevenZip_IsRefusedByName()
        {
            var sevenZip = new byte[] { 0x37, 0x7a, 0xbc, 0xaf, 0x27, 0x1c, 0x00, 0x04, 0x11, 0x22 };

            var content = await ReadArchiveAsync(sevenZip);

            Assert.AreEqual(LevelArchiveOpenResult.Unsupported, content.Result);
        }

        // What an import control can say before an author commits to a file. For a zip this is the
        // central directory rather than a decompression - the capability tar.gz gave up.
        [Test]
        [TestCase(ArchiveFormat.TarGz)]
        [TestCase(ArchiveFormat.Zip)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task PeekMeta_NamesTheLevelWithoutUnpackingIt(ArchiveFormat format)
        {
            var (plan, store) = await PlanAsync();
            var archive = await WriteArchiveAsync(plan, store, options: For(format));

            using var source = new MemoryStream(archive, writable: false);
            var peek = await LevelArchiveReader.PeekMetaAsync(source, ArchiveLimits.Default,
                CancellationToken.None);

            Assert.AreEqual(format, peek.Format);
            Assert.That(peek.IsOk, Is.True);
            Assert.That(peek.HasMeta, Is.True);
            Assert.AreEqual(plan.Meta,
                Serialization.DeserializeEnvelope<LevelMeta>(peek.MetaBytes, peek.MetaFormat));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task PeekMeta_AsksForThePassphraseRatherThanGuessing()
        {
            var (plan, store) = await PlanAsync();
            var archive = await WriteArchiveAsync(plan, store, Passphrase,
                For(ArchiveFormat.Zip, ArchiveProtection.OpenPgp));

            using var source = new MemoryStream(archive, writable: false);
            var peek = await LevelArchiveReader.PeekMetaAsync(source, ArchiveLimits.Default,
                CancellationToken.None);

            Assert.AreEqual(LevelArchiveOpenResult.PassphraseRequired, peek.Result);
            Assert.That(peek.HasMeta, Is.False);
        }

        #endregion

        #region Archive

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task Archive_RoundTrips()
        {
            var (plan, store) = await PlanAsync();
            var archive = await WriteArchiveAsync(plan, store);

            var content = await ReadArchiveAsync(archive);

            Assert.AreEqual(LevelArchiveOpenResult.Ok, content.Result);
            Assert.IsFalse(content.LevelWasProtected);
            Assert.AreEqual(plan.Level, Deserialize(content));
            Assert.AreEqual(plan.Meta, DeserializeMeta(content));

            CollectionAssert.AreEquivalent(
                new[] { "audio/song.ogg", FileNames.LogoFileNamePng, "texture.png" },
                content.ResourceFileNames);
        }

        // The documents lead, so a reader after the level card decompresses a few kilobytes rather
        // than the song - the whole mitigation for tar.gz having nothing to seek into.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task Archive_PutsTheDocumentsFirst()
        {
            var (plan, store) = await PlanAsync();
            var archive = await WriteArchiveAsync(plan, store);

            using var source = new MemoryStream(archive, writable: false);
            var payload = new MemoryContentStore("payload");
            var order = await TarGzService.UnpackAsync(source, payload,
                ArchiveLimits.Default, CancellationToken.None);

            Assert.AreEqual("metadata.json", order[0]);
            Assert.AreEqual("level.json", order[1]);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task Archive_RoundTripsInBlob()
        {
            var (plan, store) = await PlanAsync();
            var options = new LevelArchiveOptions
            {
                LevelFormat = SerializationType.Blob,
                MetaFormat = SerializationType.Blob,
            };

            var archive = await WriteArchiveAsync(plan, store, options: options);
            var content = await ReadArchiveAsync(archive);

            Assert.AreEqual(LevelArchiveOpenResult.Ok, content.Result);
            Assert.AreEqual(SerializationType.Blob, content.LevelFormat);
            Assert.AreEqual(plan.Level, Deserialize(content));
        }

        #endregion

        #region Protected archive

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task ProtectedArchive_RoundTrips()
        {
            var (plan, store) = await PlanAsync();
            var archive = await WriteArchiveAsync(plan, store, Passphrase);

            var content = await ReadArchiveAsync(archive, Passphrase);

            Assert.AreEqual(LevelArchiveOpenResult.Ok, content.Result);
            Assert.AreEqual(plan.Level, Deserialize(content));
        }

        // Three answers, not two: nobody is told they got a password wrong before they typed one.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task ProtectedArchive_AsksBeforeItRefuses()
        {
            var (plan, store) = await PlanAsync();
            var archive = await WriteArchiveAsync(plan, store, Passphrase);

            var withoutPassphrase = await ReadArchiveAsync(archive);
            var withWrongPassphrase = await ReadArchiveAsync(archive, "not it".ToCharArray());

            Assert.AreEqual(LevelArchiveOpenResult.PassphraseRequired, withoutPassphrase.Result);
            Assert.AreEqual(LevelArchiveOpenResult.WrongPassphrase, withWrongPassphrase.Result);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task ProtectedArchive_SaysWhenItIsDamaged()
        {
            var (plan, store) = await PlanAsync();
            var archive = await WriteArchiveAsync(plan, store, Passphrase);

            archive[archive.Length - 8] ^= 0xFF;
            var content = await ReadArchiveAsync(archive, Passphrase);

            Assert.AreEqual(LevelArchiveOpenResult.Damaged, content.Result);
        }

        #endregion

        #region Folder

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task Folder_RoundTrips()
        {
            var (plan, store) = await PlanAsync();
            var target = new MemoryContentStore("export");

            var written = await LevelArchiveWriter.WriteFolderAsync(plan, store, target, Serialization,
                token: CancellationToken.None);

            CollectionAssert.Contains(written, "level.json");
            CollectionAssert.Contains(written, "metadata.json");

            var content = await LevelArchiveReader.ReadAsync(target, token: CancellationToken.None);

            Assert.AreEqual(LevelArchiveOpenResult.Ok, content.Result);
            Assert.IsFalse(content.LevelWasProtected);
            Assert.AreEqual(plan.Level, Deserialize(content));
        }

        // What a protected folder promises: the CONTENT is behind a passphrase, the card is not.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task ProtectedFolder_HidesTheLevelAndLeavesTheCardReadable()
        {
            var (plan, store) = await PlanAsync();
            var target = new MemoryContentStore("export");

            await LevelArchiveWriter.WriteFolderAsync(plan, store, target, Serialization,
                passphrase: Passphrase, token: CancellationToken.None);

            Assert.IsTrue(await target.ExistsAsync("level.json.gpg", CancellationToken.None));
            Assert.IsFalse(await target.ExistsAsync("level.json", CancellationToken.None));
            Assert.IsTrue(await target.ExistsAsync("metadata.json", CancellationToken.None));
            Assert.IsTrue(await target.ExistsAsync(FileNames.LogoFileNamePng, CancellationToken.None));

            // The metadata really is plain - a browser reads it with no passphrase at all.
            var card = await LevelArchiveReader.ReadAsync(target, token: CancellationToken.None);
            Assert.AreEqual(LevelArchiveOpenResult.PassphraseRequired, card.Result);

            var content = await LevelArchiveReader.ReadAsync(target, Passphrase, CancellationToken.None);
            Assert.AreEqual(LevelArchiveOpenResult.Ok, content.Result);
            Assert.IsTrue(content.LevelWasProtected);
            Assert.AreEqual(plan.Level, Deserialize(content));
            Assert.AreEqual(plan.Meta, DeserializeMeta(content));
        }

        // PROTECTION AND FORMAT ARE INDEPENDENT, and this is the corner where nothing checked that.
        // A protected folder names its document by appending .gpg to the format's OWN extension, so
        // a blob level is level.blob.gpg - and the reader's probe has to try both formats behind the
        // encrypted name to find it. Every other protected test above runs in Json, and the blob
        // tests are all unprotected, so this combination is the one a refactor could break silently.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task ProtectedFolder_InBlob_KeepsTheFormatInTheEncryptedName()
        {
            var (plan, store) = await PlanAsync();
            var target = new MemoryContentStore("export");
            var options = new LevelArchiveOptions
            {
                LevelFormat = SerializationType.Blob,
                MetaFormat = SerializationType.Blob,
            };

            await LevelArchiveWriter.WriteFolderAsync(plan, store, target, Serialization,
                options, Passphrase, CancellationToken.None);

            Assert.IsTrue(await target.ExistsAsync("level.blob.gpg", CancellationToken.None));
            Assert.IsFalse(await target.ExistsAsync("level.blob", CancellationToken.None));
            Assert.IsFalse(await target.ExistsAsync("level.json.gpg", CancellationToken.None));

            // The card stays readable in whatever format it was written in - protection is the
            // level document's alone.
            Assert.IsTrue(await target.ExistsAsync("metadata.blob", CancellationToken.None));

            var card = await LevelArchiveReader.ReadAsync(target, token: CancellationToken.None);
            Assert.AreEqual(LevelArchiveOpenResult.PassphraseRequired, card.Result);

            var content = await LevelArchiveReader.ReadAsync(target, Passphrase, CancellationToken.None);
            Assert.AreEqual(LevelArchiveOpenResult.Ok, content.Result);
            Assert.IsTrue(content.LevelWasProtected);
            Assert.AreEqual(SerializationType.Blob, content.LevelFormat);
            Assert.AreEqual(plan.Level, Deserialize(content));
            Assert.AreEqual(plan.Meta, DeserializeMeta(content));
        }

        #endregion

        #region Refusals

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Read_SomethingThatIsNotAnArchive_SaysSo()
        {
            var content = await ReadArchiveAsync(Encoding.UTF8.GetBytes("{\"objects\":[]}"));

            Assert.AreEqual(LevelArchiveOpenResult.NotAnArchive, content.Result);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Read_AnArchiveWithNoLevelInIt_SaysItIsNotAnArchive()
        {
            var source = new MemoryContentStore("source");
            source.Write("readme.txt", Encoding.UTF8.GetBytes("just some files"));

            using var buffer = new MemoryStream();
            var entries = new List<ArchiveEntrySource>
            {
                ArchiveEntrySource.FromStore("readme.txt", source),
            };
            await TarGzService.PackAsync(entries, buffer,
                ArchivePolicy.Default, CancellationToken.None);

            var content = await ReadArchiveAsync(buffer.ToArray());

            Assert.AreEqual(LevelArchiveOpenResult.NotAnArchive, content.Result);
        }

        #endregion
    }
}