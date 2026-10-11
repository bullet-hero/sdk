using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BH.SDK.Agent;
using BH.SDK.Models;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Resources;
using BH.SDK.Serialization;
using BH.SDK.Services.Archive;
using BH.SDK.Services.Content;
using BH.SDK.Services.LevelArchive;
using BH.SDK.Utils;
using NUnit.Framework;

namespace BH.SDK.Tests.Services
{
    /// <summary> A level's <c>ai/</c> folder in an export: packed when shared, withheld when not or when
    /// protected, and a level without one writes the bytes it wrote before the folder existed. </summary>
    public class LevelArchiveAiContextTests
    {
        // Pinned with the builder as it was before ai/ existed; a change here is a format change. The tar is
        // hashed inside its gzip: BCL GZipStream compresses differently on Mono and on .NET 8.
        private const string TarWithoutAiHash =
            "sha256:c07c6910838fa29aada523aacfa29cbb5ca57dc768e15caf6f7312e1dfc8b22a";

        private const string ZipWithoutAiHash =
            "sha256:6d70ab55600d5870ed024afcca953b2239df7ca7a535cd915f91a47b95d5ba45";

        private static readonly LevelId FixedLevelId =
            new LevelId(Guid.Parse("6f0c8a52-3d4e-4b7a-9e21-5a1d2c3b4e5f"));

        private static (Level Level, LevelMeta Meta) CreateLevel()
        {
            var level = new Level();
            var meta = new LevelMeta { LevelId = FixedLevelId };

            var textureId = new TextureResourceId(1);
            level.Resources.Textures.Add(textureId, new TextureResource(textureId,
                new List<ResourceKey> { new ResourceKey(ResourceUriType.LevelPath, "resources/texture.png") }));

            var audioId = new AudioResourceId(1);
            level.Resources.Audios.Add(audioId, new AudioResource(audioId,
                new List<ResourceKey> { new ResourceKey(ResourceUriType.LevelPath, "resources/song.ogg") }));

            return (level, meta);
        }

        private static MemoryContentStore CreateLevelStore()
        {
            var store = new MemoryContentStore("level");
            store.Write("level.json", Encoding.UTF8.GetBytes("{}"));
            store.Write("metadata.json", Encoding.UTF8.GetBytes("{}"));
            store.Write(FileNames.LogoFileNamePng, Encoding.UTF8.GetBytes("a cover"));
            store.Write("resources/texture.png", Encoding.UTF8.GetBytes("a texture"));
            store.Write("resources/song.ogg", Encoding.UTF8.GetBytes("not really a song"));
            store.Write("leftover.psd", Encoding.UTF8.GetBytes("somebody's working file"));
            return store;
        }

        private static async Task<byte[]> PackAsync(IContentStore store, ArchiveFormat format)
        {
            var (level, meta) = CreateLevel();
            var plan = await LevelArchiveBuilder.BuildAsync(level, meta, store, CancellationToken.None);

            using var buffer = new MemoryStream();
            var options = new LevelArchiveOptions { Format = format };
            await LevelArchiveWriter.WriteArchiveAsync(plan, store, buffer, new SerializationService(), options,
                null, CancellationToken.None);
            return buffer.ToArray();
        }

        [TestCase(ArchiveFormat.TarGz)]
        [TestCase(ArchiveFormat.Zip)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task WithoutAiFolder_WritesThePinnedBytes(ArchiveFormat format)
        {
            var first = await PackAsync(CreateLevelStore(), format);
            var second = await PackAsync(CreateLevelStore(), format);

            Assert.AreEqual(first, second, "the fixture itself must be reproducible");

            var expected = format == ArchiveFormat.TarGz ? TarWithoutAiHash : ZipWithoutAiHash;
            var pinned = format == ArchiveFormat.TarGz ? Gunzip(first) : first;
            Assert.AreEqual(expected, ContentHashUtils.Sha256(pinned));
        }

        private static byte[] Gunzip(byte[] archive)
        {
            using var gzip = new GZipStream(new MemoryStream(archive, false), CompressionMode.Decompress);
            using var tar = new MemoryStream();
            gzip.CopyTo(tar);
            return tar.ToArray();
        }

        private const string ContextPath = "ai/CONTEXT.md";
        private const string ManifestPath = "ai/manifest.json";
        private const string WithheldCode = "archive.ai_context_withheld";

        private static MemoryContentStore CreateStoreWithAi(string manifestJson)
        {
            var store = CreateLevelStore();
            store.Write(ContextPath, Encoding.UTF8.GetBytes("# Brief\nA volcano level.\n"));
            if (manifestJson != null) store.Write(ManifestPath, Encoding.UTF8.GetBytes(manifestJson));
            return store;
        }

        private static Task<LevelArchivePlan> PlanAsync(IContentStore store,
            LevelExportMode mode = LevelExportMode.Folder)
        {
            var (level, meta) = CreateLevel();
            return LevelArchiveBuilder.BuildAsync(level, meta, store, mode, CancellationToken.None);
        }

        private static List<string> Packed(LevelArchivePlan plan) =>
            plan.Files.Select(file => file.ArchivePath).ToList();

        private static bool HasCode(LevelArchivePlan plan, string code) =>
            plan.Report.Issues.Any(issue => issue.Code == code);

        [TestCase(ArchiveFormat.TarGz)]
        [TestCase(ArchiveFormat.Zip)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task SharedAiFolder_IsPacked_AndReadBack(ArchiveFormat format)
        {
            var store = CreateStoreWithAi("{ \"format\": 1, \"share\": true }");

            var plan = await PlanAsync(store);

            CollectionAssert.IsSubsetOf(new[] { ContextPath, ManifestPath }, Packed(plan));
            Assert.AreEqual(1, plan.DroppedFileCount, "only leftover.psd is left behind");
            Assert.IsFalse(HasCode(plan, WithheldCode));

            using var buffer = new MemoryStream();
            await LevelArchiveWriter.WriteArchiveAsync(plan, store, buffer, new SerializationService(),
                new LevelArchiveOptions { Format = format }, null, CancellationToken.None);

            var content = await LevelArchiveReader.ReadAsync(new MemoryStream(buffer.ToArray(), false), null,
                token: CancellationToken.None);

            Assert.AreEqual(LevelArchiveOpenResult.Ok, content.Result);
            CollectionAssert.Contains(content.ResourceFileNames, ContextPath);
            CollectionAssert.Contains(content.ResourceFileNames, ManifestPath);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task ManifestWithoutShare_StillShares()
        {
            var plan = await PlanAsync(CreateStoreWithAi("{ \"format\": 1 }"));

            CollectionAssert.Contains(Packed(plan), ContextPath);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task ShareFalse_IsWithheld_AndNotCountedAsDropped()
        {
            var plan = await PlanAsync(CreateStoreWithAi("{ \"format\": 1, \"share\": false }"));

            Assert.IsFalse(Packed(plan).Any(path => path.StartsWith("ai/", StringComparison.Ordinal)));
            Assert.IsTrue(HasCode(plan, WithheldCode));
            Assert.AreEqual(1, plan.DroppedFileCount, "the withheld folder is reported apart from leftovers");
        }

        [TestCase(LevelExportMode.FolderProtectedLevel)]
        [TestCase(LevelExportMode.TarGzProtected)]
        [TestCase(LevelExportMode.ZipProtected)]
        [TestCase(LevelExportMode.ZipEncrypted)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task ProtectedMode_WithholdsASharedFolder(LevelExportMode mode)
        {
            var plan = await PlanAsync(CreateStoreWithAi("{ \"format\": 1, \"share\": true }"), mode);

            Assert.IsFalse(Packed(plan).Any(path => path.StartsWith("ai/", StringComparison.Ordinal)));
            Assert.IsTrue(HasCode(plan, WithheldCode));
            Assert.AreEqual(1, plan.DroppedFileCount);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task UnreadableManifest_IsWithheld()
        {
            var plan = await PlanAsync(CreateStoreWithAi("{ not json"));

            CollectionAssert.DoesNotContain(Packed(plan), ContextPath);
            Assert.IsTrue(HasCode(plan, WithheldCode));
        }

        // No manifest means no AI wrote the folder; it is left to the ordinary leftover count.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task NoManifest_IsNotPacked_AndCountsAsLeftover()
        {
            var plan = await PlanAsync(CreateStoreWithAi(null));

            CollectionAssert.DoesNotContain(Packed(plan), ContextPath);
            Assert.IsFalse(HasCode(plan, WithheldCode));
            Assert.AreEqual(2, plan.DroppedFileCount);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task FilesBreakingTheRules_StayBehind()
        {
            var store = CreateStoreWithAi("{ \"format\": 1, \"share\": true }");
            store.Write("ai/script.py", Encoding.UTF8.GetBytes("print()"));
            store.Write("ai/sub/notes.md", Encoding.UTF8.GetBytes("nested"));
            store.Write("ai/huge.md", new byte[AgentRules.MaxContextFileBytes + 1]);

            var plan = await PlanAsync(store);

            var packed = Packed(plan);
            CollectionAssert.Contains(packed, ContextPath);
            CollectionAssert.DoesNotContain(packed, "ai/script.py");
            CollectionAssert.DoesNotContain(packed, "ai/sub/notes.md");
            CollectionAssert.DoesNotContain(packed, "ai/huge.md");
            Assert.AreEqual(4, plan.DroppedFileCount, "leftover.psd plus the three refused files");
        }
    }
}
