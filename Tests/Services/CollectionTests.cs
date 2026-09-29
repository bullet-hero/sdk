using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BH.SDK.Models;
using BH.SDK.Models.Data;
using BH.SDK.Models.Enums.Resources;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Meta;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Services.Archive;
using BH.SDK.Services.Collections;
using BH.SDK.Services.Content;
using BH.SDK.Utils;
using NUnit.Framework;

namespace BH.SDK.Tests.Services
{
    // THE WHOLE COLLECTION PIPELINE WITH NO DISK: a level's prefab exported into a collection, the
    // collection written, archived, unpacked and read back, then imported into a second level whose ids
    // are already taken. The critical case is the last one - a prefab that nests a template and draws a
    // custom shape with a texture, arriving where texture -1 exists and the prefab's own id holds
    // something different. Every id the import moves must be moved everywhere it is referenced.
    public class CollectionTests
    {
        private static readonly SerializationService Serialization = new();
        private static readonly byte[] TextureBytes = Encoding.UTF8.GetBytes("not really a png");

        private sealed class SourceLevel
        {
            public readonly LevelResources Resources = new();
            public readonly LevelMeta Meta = new();
            public readonly MemoryContentStore Folder = new("level");
            public readonly Prefab Outer = new() { PrefabId = PrefabId.NewId() };
            public readonly Prefab Nested = new() { PrefabId = PrefabId.NewId() };
            public readonly CompositeShape Shape = new() { ShapeId = new ShapeId(Guid.NewGuid()) };
            public readonly TextureResource Texture = new() { TextureResourceId = new TextureResourceId(-1) };

            public SourceLevel()
            {
                Texture.Sources.Add(new ResourceKey(ResourceUriType.LevelPath, "tex.png"));
                Resources.Textures.Add(Texture.TextureResourceId, Texture);
                Resources.CompositeShapes.Add(Shape.ShapeId, Shape);

                var drawn = new ShapeObject
                {
                    ObjectId = new ObjectId(1), ShapeId = Shape.ShapeId, TextureResourceId = Texture.TextureResourceId,
                };
                Nested.Objects.Add(drawn.ObjectId, drawn);
                Resources.Prefabs.Add(Nested.PrefabId, Nested);

                var placement = new PrefabObject { ObjectId = new ObjectId(1), PrefabId = Nested.PrefabId };
                Outer.Objects.Add(placement.ObjectId, placement);
                Resources.Prefabs.Add(Outer.PrefabId, Outer);

                Meta.ResourcesMeta.Add(new ResourceMeta(ResourceType.Texture, new TypedResourceId(-1),
                    new StringValue("a texture"), new StringValue(), string.Empty, new NoSpecifiedLicense(),
                    new List<IString>(), new List<Author>()));
                Meta.ResourcesMeta.Add(new ResourceMeta(ResourceType.Prefab, TypedResourceId.Null,
                    new StringValue("a prefab"), new StringValue(), string.Empty, new NoSpecifiedLicense(),
                    new List<IString>(), new List<Author>(), resourceGuid: Outer.PrefabId.value));
            }
        }

        private static async Task Write(IContentStore store, string path, byte[] bytes)
        {
            using var stream = await store.OpenWriteAsync(path, CancellationToken.None);
            await stream.WriteAsync(bytes, 0, bytes.Length);
        }

        private static async Task<byte[]> Read(IContentStore store, string path)
        {
            using var stream = await store.OpenReadAsync(path, CancellationToken.None);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            return buffer.ToArray();
        }

        // Export the outer prefab and write the collection, copying the one media file the export
        // reports - what the host does on disk.
        private static async Task<MemoryContentStore> ExportAsync(SourceLevel level)
        {
            await Write(level.Folder, "tex.png", TextureBytes);

            var store = new MemoryContentStore("collection");
            var content = CollectionContent.CreateEmpty(store, SerializationType.Json);
            content.Manifest.Name = new StringValue("pack");

            var export = CollectionExportPlanner.Export(level.Resources, level.Meta,
                new[] { ResourceRef.Of(level.Outer.PrefabId) }, content);

            var media = export.Files.Select(file =>
                ArchiveEntrySource.FromStore(file.DestinationPath, level.Folder, file.Source.Uri)).ToList();
            await CollectionWriter.WriteAsync(store, content, media, Serialization, SerializationType.Json);
            return store;
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task Export_WritesTheWholeClosure_AndReadsBack()
        {
            var level = new SourceLevel();
            var store = await ExportAsync(level);

            var outcome = await CollectionReader.ReadAsync(store, Serialization);

            Assert.IsTrue(outcome.IsOk);
            var resources = outcome.Content.Resources;
            CollectionAssert.AreEquivalent(new[] { level.Outer.PrefabId, level.Nested.PrefabId },
                resources.Prefabs.Keys);
            CollectionAssert.AreEquivalent(new[] { level.Shape.ShapeId }, resources.CompositeShapes.Keys);
            Assert.AreEqual(1, resources.Textures.Count);

            var texture = resources.Textures.Values.Single();
            Assert.AreEqual("media/tex.png", texture.Sources.Single().Uri);
            CollectionAssert.AreEqual(TextureBytes, await Read(store, "media/tex.png"));

            var drawn = (ShapeObject)resources.Prefabs[level.Nested.PrefabId].Objects.Values.Single();
            Assert.AreEqual(texture.TextureResourceId, drawn.TextureResourceId);
            Assert.AreEqual(2, outcome.Content.Manifest.ResourcesMeta.Count);
            Assert.IsEmpty(outcome.Content.Skipped);
        }

        [TestCase(ArchiveFormat.Zip)]
        [TestCase(ArchiveFormat.TarGz)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task Archive_RoundTripsTheCollection(ArchiveFormat format)
        {
            var level = new SourceLevel();
            var store = await ExportAsync(level);

            using var archive = new MemoryStream();
            await CollectionArchive.PackAsync(store, archive, format);
            archive.Position = 0;

            var (result, content) = await CollectionArchive.UnpackAsync(archive, new MemoryContentStore("into"),
                Serialization);

            Assert.AreEqual(CollectionArchiveResult.Ok, result);
            Assert.AreEqual(2, content.Resources.Prefabs.Count);
            CollectionAssert.AreEqual(TextureBytes, await Read(content.Store, "media/tex.png"));
        }

        [TestCase(ArchiveFormat.Zip, ArchiveProtection.ZipAes256)]
        [TestCase(ArchiveFormat.Zip, ArchiveProtection.OpenPgp)]
        [TestCase(ArchiveFormat.TarGz, ArchiveProtection.OpenPgp)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task EncryptedArchive_AsksForThePassphrase_RefusesAWrongOne_OpensWithTheRightOne(
            ArchiveFormat format, ArchiveProtection protection)
        {
            var store = await ExportAsync(new SourceLevel());

            using var archive = new MemoryStream();
            await CollectionArchive.PackAsync(store, archive, format, protection, "пароль".ToCharArray());

            archive.Position = 0;
            var (missing, _) = await CollectionArchive.UnpackAsync(archive, new MemoryContentStore("a"), Serialization);
            Assert.AreEqual(CollectionArchiveResult.PassphraseRequired, missing);

            archive.Position = 0;
            var (wrong, _) = await CollectionArchive.UnpackAsync(archive, new MemoryContentStore("b"), Serialization,
                "wrong".ToCharArray());
            Assert.AreEqual(CollectionArchiveResult.WrongPassphrase, wrong);

            archive.Position = 0;
            var (result, content) = await CollectionArchive.UnpackAsync(archive, new MemoryContentStore("c"),
                Serialization, "пароль".ToCharArray());
            Assert.AreEqual(CollectionArchiveResult.Ok, result);
            Assert.AreEqual(2, content.Resources.Prefabs.Count);
            CollectionAssert.AreEqual(TextureBytes, await Read(content.Store, "media/tex.png"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task AFolderWithNoManifest_IsNotACollection()
        {
            var store = new MemoryContentStore("empty");
            await Write(store, "prefabs/readme.txt", TextureBytes);

            var outcome = await CollectionReader.ReadAsync(store, Serialization);

            Assert.AreEqual(CollectionReadResult.NotACollection, outcome.Result);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task AnEntryUnderAnotherIdsName_IsSkipped()
        {
            var level = new SourceLevel();
            var store = await ExportAsync(level);

            var misnamed =
                CollectionReader.EntryPath(FileNames.PrefabsDirectory, Guid.NewGuid(), SerializationType.Json);
            await Write(store, misnamed, await Read(store, CollectionReader.EntryPath(FileNames.PrefabsDirectory,
                level.Outer.PrefabId.value, SerializationType.Json)));

            var outcome = await CollectionReader.ReadAsync(store, Serialization);

            Assert.IsTrue(outcome.IsOk);
            Assert.AreEqual(1, outcome.Content.Skipped.Count);
            Assert.AreEqual(CollectionEntryProblem.IdMismatch, outcome.Content.Skipped[0].Problem);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public async Task Import_IntoTakenIds_MovesEveryReference()
        {
            var level = new SourceLevel();
            var outcome = await CollectionReader.ReadAsync(await ExportAsync(level), Serialization);
            var collection = outcome.Content;

            // The target already has texture -1 and a DIFFERENT template under the outer prefab's id,
            // and a file called tex.png in its folder.
            var target = new LevelResources();
            var theirs = new TextureResource { TextureResourceId = new TextureResourceId(-1) };
            target.Textures.Add(theirs.TextureResourceId, theirs);
            var clash = new Prefab { PrefabId = level.Outer.PrefabId };
            target.Prefabs.Add(clash.PrefabId, clash);

            var plan = CollectionImportPlanner.Plan(target, new LevelMeta(), collection,
                new[] { ResourceRef.Of(level.Outer.PrefabId) }, new[] { "tex.png" });

            Assert.IsTrue(plan.HasConflicts);
            Assert.AreEqual(3, plan.DependencyCount); // nested template, shape, texture

            var result = plan.Resolve(new Dictionary<ResourceRef, ConflictAnswer>
            {
                [ResourceRef.Of(level.Outer.PrefabId)] = ConflictAnswer.ImportAsCopy,
            });

            var texture = result.Added.Textures.Values.Single();
            Assert.AreEqual(-2, texture.TextureResourceId.value);
            Assert.AreEqual("tex_1.png", texture.Sources.Single().Uri);
            Assert.AreEqual("tex_1.png", result.Files.Single().DestinationFileName);

            var outerCopy = result.Added.Prefabs.Values.Single(p => p.PrefabId != level.Nested.PrefabId);
            Assert.AreNotEqual(level.Outer.PrefabId, outerCopy.PrefabId);

            var nested = result.Added.Prefabs[level.Nested.PrefabId];
            var drawn = (ShapeObject)nested.Objects.Values.Single();
            Assert.AreEqual(texture.TextureResourceId, drawn.TextureResourceId);

            // The prefab credit follows the copy's new id, the texture credit the new texture id.
            Assert.IsTrue(result.Meta.Any(m =>
                m.ResourceType == ResourceType.Prefab && m.ResourceGuid == outerCopy.PrefabId.value));
            Assert.IsTrue(result.Meta.Any(m => m.ResourceType == ResourceType.Texture && m.ResourceId.value == -2));

            // The collection itself is untouched.
            Assert.IsTrue(collection.Resources.Prefabs.ContainsKey(level.Outer.PrefabId));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Import_OfWhatTheLevelHasIdentically_AddsNoDataResource()
        {
            var level = new SourceLevel();
            var collection = (await CollectionReader.ReadAsync(await ExportAsync(level), Serialization)).Content;

            var target = new LevelResources();
            target.CompositeShapes.Add(level.Shape.ShapeId,
                collection.Resources.CompositeShapes[level.Shape.ShapeId].Copy());

            var plan = CollectionImportPlanner.Plan(target, new LevelMeta(), collection,
                new[] { ResourceRef.Of(level.Shape.ShapeId) }, Array.Empty<string>());

            Assert.AreEqual(ImportEntryState.Identical, plan.Entries.Single().State);
            Assert.AreEqual(0, plan.Resolve().Count);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Import_OfAFileTheLevelHasByteForByte_ReusesTheLevelsOwn()
        {
            var level = new SourceLevel();
            var collection = (await CollectionReader.ReadAsync(await ExportAsync(level), Serialization)).Content;
            var reference = ResourceRef.Of(collection.Resources.Textures.Keys.Single());

            // The target holds the same bytes under another name and id, and a different file at -1.
            var folder = new MemoryContentStore("target");
            await Write(folder, "same.png", TextureBytes);
            await Write(folder, "other.png", Encoding.UTF8.GetBytes("a different picture"));
            var target = new LevelResources();
            var other = new TextureResource { TextureResourceId = new TextureResourceId(-1) };
            other.Sources.Add(new ResourceKey(ResourceUriType.LevelPath, "other.png"));
            target.Textures.Add(other.TextureResourceId, other);
            var same = new TextureResource { TextureResourceId = new TextureResourceId(-7) };
            same.Sources.Add(new ResourceKey(ResourceUriType.LevelPath, "same.png"));
            target.Textures.Add(same.TextureResourceId, same);

            var plan = CollectionImportPlanner.Plan(target, new LevelMeta(), collection, new[] { reference },
                new[] { "same.png", "other.png" },
                await MediaFingerprints.ComputeAsync(collection.Resources, collection.Store),
                await MediaFingerprints.ComputeAsync(target, folder));
            var result = plan.Resolve();

            Assert.AreEqual(1, plan.Reused.Count);
            Assert.AreEqual(0, result.Added.Textures.Count);
            Assert.AreEqual(0, result.Files.Count);
            Assert.AreEqual(-7, result.Remap.Map(reference).Id);
            // The credit the collection carries lands on the level's own texture.
            Assert.IsTrue(result.Meta.Any(m => m.ResourceType == ResourceType.Texture && m.ResourceId.value == -7));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public async Task Import_OfAFileWithDifferentBytes_IsStillCopied()
        {
            var level = new SourceLevel();
            var collection = (await CollectionReader.ReadAsync(await ExportAsync(level), Serialization)).Content;
            var reference = ResourceRef.Of(collection.Resources.Textures.Keys.Single());

            var folder = new MemoryContentStore("target");
            await Write(folder, "tex.png", Encoding.UTF8.GetBytes("a different picture"));
            var target = new LevelResources();
            var theirs = new TextureResource { TextureResourceId = new TextureResourceId(-1) };
            theirs.Sources.Add(new ResourceKey(ResourceUriType.LevelPath, "tex.png"));
            target.Textures.Add(theirs.TextureResourceId, theirs);

            var plan = CollectionImportPlanner.Plan(target, new LevelMeta(), collection, new[] { reference },
                new[] { "tex.png" },
                await MediaFingerprints.ComputeAsync(collection.Resources, collection.Store),
                await MediaFingerprints.ComputeAsync(target, folder));
            var result = plan.Resolve();

            Assert.AreEqual(0, plan.Reused.Count);
            Assert.AreEqual(1, result.Added.Textures.Count);
            Assert.AreEqual("tex_1.png", result.Files.Single().DestinationFileName);
        }
    }
}