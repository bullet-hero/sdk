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
using BH.SDK.Services.Package;
using NUnit.Framework;

namespace BH.SDK.Tests.Services
{
    // THE WHOLE PIPELINE, WITH NO DISK ANYWHERE - which is the server's path exactly. A backend
    // accepts a tar.gz, hands it to the reader and gets a store back; nothing in that sentence
    // mentions a file system, and these tests are what makes it true rather than aspirational.
    //
    // The four export modes are two independent choices, and the pair of them is what is checked
    // here: a protected ARCHIVE is opaque from the outside, while a protected FOLDER encrypts the
    // level document alone and leaves the metadata and the cover readable - so a browser can still
    // draw the card and ask for the passphrase only when the level is opened. Those are different
    // promises, and each of them is worth a test that would fail if the other were implemented.
    public class LevelPackageRoundTripTests
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

        private static async Task<(LevelPackagePlan Plan, MemoryContentStore Store)> PlanAsync()
        {
            var (level, meta) = CreateLevel();
            var store = CreateLevelStore();
            var plan = await LevelPackageBuilder.BuildAsync(level, meta, store, CancellationToken.None);
            return (plan, store);
        }

        private static async Task<byte[]> WriteArchiveAsync(LevelPackagePlan plan, IContentStore source,
            char[] passphrase = null, LevelPackageOptions options = null)
        {
            using var buffer = new MemoryStream();
            await LevelPackageWriter.WriteArchiveAsync(plan, source, buffer, Serialization, options,
                passphrase, CancellationToken.None);
            return buffer.ToArray();
        }

        private static Task<LevelPackageContent> ReadArchiveAsync(byte[] archive, char[] passphrase = null)
        {
            var source = new MemoryStream(archive, writable: false);
            return LevelPackageReader.ReadAsync(source, passphrase, token: CancellationToken.None);
        }

        private static Level Deserialize(LevelPackageContent content) =>
            Serialization.DeserializeEnvelope<Level>(content.LevelBytes, content.LevelFormat);

        private static LevelMeta DeserializeMeta(LevelPackageContent content) =>
            Serialization.DeserializeEnvelope<LevelMeta>(content.MetaBytes, content.MetaFormat);

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

            Assert.AreEqual(LevelPackageOpenResult.Ok, content.Result);
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
            var options = new LevelPackageOptions
            {
                LevelFormat = SerializationType.Blob,
                MetaFormat = SerializationType.Blob,
            };

            var archive = await WriteArchiveAsync(plan, store, options: options);
            var content = await ReadArchiveAsync(archive);

            Assert.AreEqual(LevelPackageOpenResult.Ok, content.Result);
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

            Assert.AreEqual(LevelPackageOpenResult.Ok, content.Result);
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

            Assert.AreEqual(LevelPackageOpenResult.PassphraseRequired, withoutPassphrase.Result);
            Assert.AreEqual(LevelPackageOpenResult.WrongPassphrase, withWrongPassphrase.Result);
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

            Assert.AreEqual(LevelPackageOpenResult.Damaged, content.Result);
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

            var written = await LevelPackageWriter.WriteFolderAsync(plan, store, target, Serialization,
                token: CancellationToken.None);

            CollectionAssert.Contains(written, "level.json");
            CollectionAssert.Contains(written, "metadata.json");

            var content = await LevelPackageReader.ReadAsync(target, token: CancellationToken.None);

            Assert.AreEqual(LevelPackageOpenResult.Ok, content.Result);
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

            await LevelPackageWriter.WriteFolderAsync(plan, store, target, Serialization,
                passphrase: Passphrase, token: CancellationToken.None);

            Assert.IsTrue(await target.ExistsAsync("level.json.gpg", CancellationToken.None));
            Assert.IsFalse(await target.ExistsAsync("level.json", CancellationToken.None));
            Assert.IsTrue(await target.ExistsAsync("metadata.json", CancellationToken.None));
            Assert.IsTrue(await target.ExistsAsync(FileNames.LogoFileNamePng, CancellationToken.None));

            // The metadata really is plain - a browser reads it with no passphrase at all.
            var card = await LevelPackageReader.ReadAsync(target, token: CancellationToken.None);
            Assert.AreEqual(LevelPackageOpenResult.PassphraseRequired, card.Result);

            var content = await LevelPackageReader.ReadAsync(target, Passphrase, CancellationToken.None);
            Assert.AreEqual(LevelPackageOpenResult.Ok, content.Result);
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
            var options = new LevelPackageOptions
            {
                LevelFormat = SerializationType.Blob,
                MetaFormat = SerializationType.Blob,
            };

            await LevelPackageWriter.WriteFolderAsync(plan, store, target, Serialization,
                options, Passphrase, CancellationToken.None);

            Assert.IsTrue(await target.ExistsAsync("level.blob.gpg", CancellationToken.None));
            Assert.IsFalse(await target.ExistsAsync("level.blob", CancellationToken.None));
            Assert.IsFalse(await target.ExistsAsync("level.json.gpg", CancellationToken.None));

            // The card stays readable in whatever format it was written in - protection is the
            // level document's alone.
            Assert.IsTrue(await target.ExistsAsync("metadata.blob", CancellationToken.None));

            var card = await LevelPackageReader.ReadAsync(target, token: CancellationToken.None);
            Assert.AreEqual(LevelPackageOpenResult.PassphraseRequired, card.Result);

            var content = await LevelPackageReader.ReadAsync(target, Passphrase, CancellationToken.None);
            Assert.AreEqual(LevelPackageOpenResult.Ok, content.Result);
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
        public async Task Read_SomethingThatIsNotAPackage_SaysSo()
        {
            var content = await ReadArchiveAsync(Encoding.UTF8.GetBytes("{\"objects\":[]}"));

            Assert.AreEqual(LevelPackageOpenResult.NotAPackage, content.Result);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Read_AnArchiveWithNoLevelInIt_SaysItIsNotAPackage()
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

            Assert.AreEqual(LevelPackageOpenResult.NotAPackage, content.Result);
        }

        #endregion
    }
}