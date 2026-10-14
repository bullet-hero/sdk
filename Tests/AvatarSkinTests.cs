using System;
using System.Collections.Generic;
using System.Linq;
using BH.SDK.Models;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Profile;
using BH.SDK.Models.Values;
using BH.SDK.Serialization;
using BH.SDK.Services.Profile;
using BH.SDK.Services.Skins;
using BH.SDK.Utils;
using BH.SDK.Validations;
using BH.SDK.Versions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    /// <summary> The avatar skin file: its round trip, what a hand-written file may leave out, what
    /// validation refuses, the mask, the ids and file names, and the profile manifest's generation 3.
    /// </summary>
    [TestFixture]
    public class AvatarSkinTests
    {
        private static readonly ShapeId MeshId = new(new Guid("4d1f6c2a-7b3e-4f10-9a55-2c8e01b7d001"));

        private static AvatarSkinShape Triangle() => new(MeshId,
            new List<Vector2Value> { new(-0.5f, -0.5f), new(0.5f, -0.5f), new(0f, 0.5f) },
            new List<int> { 0, 1, 2 });

        // Rows top first, '1' filled - only a way to write a mask readably in a test.
        private static AvatarSkin Skin(params string[] rows)
        {
            var skin = new AvatarSkin { Id = AvatarSkinId.NewId(), Name = "test", Rows = rows.Length, Mask = 0UL };
            skin.Columns = rows[0].Length;
            for (var y = 0; y < rows.Length; y++)
            for (var x = 0; x < rows[y].Length; x++)
                if (rows[rows.Length - 1 - y][x] == '1')
                    skin.Mask |= AvatarSkinMask.Bit(x, y);
            return skin;
        }

        private static void AssertValid(AvatarSkin skin) =>
            CollectionAssert.IsEmpty(AvatarSkinValidation.Validate(skin), "a drawable skin reports nothing");

        private static void AssertOneError(AvatarSkin skin)
        {
            var errors = AvatarSkinValidation.Validate(skin);
            Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ASkin_RoundTripsThroughJson()
        {
            var service = new SerializationService();
            var skin = Skin("0110", "1111", "1111", "0110");
            skin.Size = 0.8f;
            skin.Colors.Add(new Color4Value(0.1f, 0.2f, 0.3f, 1f));
            skin.Parts.Add(new AvatarSkinPart(MeshId, 1) { Rotation = 90f, Mirror = true, Scale = 0.5f });
            skin.Meshes.Add(Triangle());
            skin.Cells = Enumerable.Range(0, AvatarSkinMask.Count(skin.Mask)).Select(i => i % 2).ToList();
            skin.Elements = AvatarSkinElements.Eyes | AvatarSkinElements.DamageEffect;

            var json = service.SerializeData(skin);
            var read = service.DeserializeData<AvatarSkin>(json);

            Assert.AreEqual(skin, read);
            Assert.AreEqual(ModelGenerations.V3_ImproveAppearance, (int)JObject.Parse(json)[Names.Generation]);
            AssertValid(read);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AFullEightByEightMask_RoundTripsEveryBit()
        {
            var service = new SerializationService();
            var skin = Skin(Enumerable.Repeat("11111111", 8).ToArray());

            var read = service.DeserializeData<AvatarSkin>(service.SerializeData(skin));

            Assert.AreEqual(ulong.MaxValue, read.Mask);
            AssertValid(read);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void AFreshSkin_IsTheClassicAvatarAndNeedsOnlyAnId()
        {
            var skin = new AvatarSkin();

            Assert.AreEqual(AvatarSkin.ClassicSide, skin.Columns);
            Assert.AreEqual(AvatarSkin.ClassicSide, skin.Rows);
            Assert.AreEqual(AvatarSkin.ClassicSide * AvatarSkin.ClassicSide, AvatarSkinMask.Count(skin.Mask));
            Assert.AreEqual(ShapeId.Square.Fill, skin.Parts.Single().Shape);
            Assert.AreEqual(AvatarSkinElements.Classic, skin.Elements);
            AssertOneError(skin);

            skin.Id = AvatarSkinId.Classic;
            AssertValid(skin);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AMinimalHandWrittenFile_KeepsEveryOmittedMemberAtItsDefault()
        {
            var json = $"{{\"{Names.Generation}\":{ModelGenerations.V3_ImproveAppearance},\"{Names.ValueShort}\":{{" +
                       "\"id\":\"9b2f0d64-1c3a-4e8b-8f21-6a0c5d7e9f13\",\"name\":\"mini\"," +
                       "\"columns\":3,\"rows\":3,\"mask\":" + (AvatarSkinMask.Full(3, 3) & ~AvatarSkinMask.Bit(1, 1)) +
                       ",\"elements\":" + (int)AvatarSkinElements.Shadow + "}}";

            var read = new SerializationService().DeserializeData<AvatarSkin>(json);
            var fresh = new AvatarSkin();

            Assert.AreEqual("mini", read.Name);
            Assert.AreEqual(8, AvatarSkinMask.Count(read.Mask));
            Assert.AreEqual(AvatarSkinElements.Shadow, read.Elements);
            Assert.AreEqual(fresh.Size, read.Size);
            Assert.AreEqual(fresh.Parts, read.Parts);
            Assert.AreEqual(fresh.Colors, read.Colors);
            Assert.AreEqual(fresh.Eyes, read.Eyes);
            Assert.AreEqual(fresh.DashBody, read.DashBody);
            CollectionAssert.IsEmpty(read.Cells);
            CollectionAssert.IsEmpty(read.Meshes);
            AssertValid(read);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TheGridEdges_AreValid()
        {
            AssertValid(Skin("111", "111", "111"));
            AssertValid(Skin("111", "111", "111", "111", "111", "111", "111", "111"));
            AssertValid(Skin("11111111", "10000001", "11111111"));

            var mesh = Skin("010", "111", "010");
            mesh.Parts.Add(new AvatarSkinPart(MeshId, 0));
            mesh.Meshes.Add(Triangle());
            mesh.Cells = new List<int> { 1, 0, 1, 0, 1 };
            AssertValid(mesh);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void EachBrokenRule_ReportsExactlyOnce()
        {
            var noId = Skin("111", "111", "111");
            noId.Id = AvatarSkinId.Null;
            AssertOneError(noId);

            AssertOneError(Skin("11", "11", "11"));
            AssertOneError(Skin("100", "000", "001"));
            AssertOneError(Skin("11011", "11011", "11011"));

            var outside = Skin("111", "111", "111");
            outside.Mask |= AvatarSkinMask.Bit(3, 0);
            AssertOneError(outside);

            var cellCount = Skin("111", "111", "111");
            cellCount.Cells = new List<int> { 0, 0 };
            AssertOneError(cellCount);

            var cellPart = Skin("111", "111", "111");
            cellPart.Cells = Enumerable.Repeat(0, 8).Append(1).ToList();
            AssertOneError(cellPart);

            var noParts = Skin("111", "111", "111");
            noParts.Parts.Clear();
            AssertOneError(noParts);

            var unknownShape = Skin("111", "111", "111");
            unknownShape.Parts[0].Shape = MeshId;
            AssertOneError(unknownShape);

            var color = Skin("111", "111", "111");
            color.Parts[0].Color = 1;
            AssertOneError(color);

            var noColors = Skin("111", "111", "111");
            noColors.Colors.Clear();
            noColors.Parts[0].Color = 0;
            Assert.AreEqual(2, AvatarSkinValidation.Validate(noColors).Count, "the list and the part both report");

            var catalogueMesh = Skin("111", "111", "111");
            catalogueMesh.Meshes.Add(Triangle());
            catalogueMesh.Meshes[0].Id = ShapeId.Square.Fill;
            AssertOneError(catalogueMesh);

            var twinMeshes = Skin("111", "111", "111");
            twinMeshes.Meshes.Add(Triangle());
            twinMeshes.Meshes.Add(Triangle());
            AssertOneError(twinMeshes);

            var geometry = Skin("111", "111", "111");
            geometry.Meshes.Add(
                new AvatarSkinShape(MeshId, new List<Vector2Value> { new(2f, 2f) }, new List<int> { 0 }));
            AssertOneError(geometry);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TheMask_CountsRowsFromTheBottomWithAStrideOfEight()
        {
            Assert.AreEqual(1UL, AvatarSkinMask.Bit(0, 0));
            Assert.AreEqual(1UL << 8, AvatarSkinMask.Bit(0, 1));
            Assert.AreEqual(1UL << 63, AvatarSkinMask.Bit(7, 7));
            Assert.AreEqual(0x0707UL | (0x07UL << 16), AvatarSkinMask.Full(3, 3));
            Assert.AreEqual(64, AvatarSkinMask.Count(ulong.MaxValue));

            var ring = AvatarSkinMask.Full(3, 3) & ~AvatarSkinMask.Bit(1, 1);
            Assert.AreEqual(-1, AvatarSkinMask.RankOf(ring, 1, 1));
            Assert.AreEqual(3, AvatarSkinMask.RankOf(ring, 0, 1));
            Assert.AreEqual(4, AvatarSkinMask.RankOf(ring, 2, 1));
            Assert.AreEqual(7, AvatarSkinMask.RankOf(ring, 2, 2));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void PartShapes_ResolveFromTheCatalogueOrTheSkinsOwnMeshes()
        {
            Assert.IsTrue(AvatarSkinShapes.TryGet("Square", out var square));
            Assert.AreEqual(ShapeId.Square.Fill, square);
            Assert.IsTrue(AvatarSkinShapes.IsCatalogue(square));
            Assert.IsFalse(AvatarSkinShapes.IsCatalogue(MeshId));
            Assert.IsFalse(AvatarSkinShapes.IsCatalogue(ShapeId.Null));
            Assert.IsFalse(AvatarSkinShapes.TryGet("square", out _), "names are case-sensitive");

            var skin = Skin("111", "111", "111");
            skin.Meshes.Add(Triangle());
            Assert.AreSame(skin.Meshes[0], AvatarSkinShapes.Geometry(skin, MeshId));
            Assert.IsNotNull(AvatarSkinShapes.Geometry(skin, square));
            Assert.IsNull(AvatarSkinShapes.Geometry(new AvatarSkin(), MeshId));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void SkinIds_NameTheirFilesAndKeepBuiltinsApart()
        {
            var id = new AvatarSkinId("9b2f0d64-1c3a-4e8b-8f21-6a0c5d7e9f13");

            Assert.IsTrue(AvatarSkinId.Classic.IsBuiltin());
            Assert.IsTrue(AvatarSkinGenerator.PresetId(7).IsBuiltin());
            Assert.IsFalse(AvatarSkinId.NewId().IsBuiltin());
            Assert.IsFalse(AvatarSkinId.Null.IsBuiltin());

            Assert.AreEqual("9b2f0d64-1c3a-4e8b-8f21-6a0c5d7e9f13.json", AvatarSkinFiles.FileNameOf(id));
            Assert.IsNull(AvatarSkinFiles.FileNameOf(AvatarSkinId.Classic));
            Assert.IsTrue(AvatarSkinFiles.TryGetId("9b2f0d64-1c3a-4e8b-8f21-6a0c5d7e9f13.json", out var parsed));
            Assert.AreEqual(id, parsed);
            Assert.IsFalse(AvatarSkinFiles.TryGetId("my-skin.json", out _));
            Assert.IsTrue(AvatarSkinFiles.IsSkinFile("my-skin.json"));
            Assert.IsFalse(AvatarSkinFiles.IsSkinFile(FileNames.SkinSelectionFileName));
            Assert.IsFalse(AvatarSkinFiles.IsSkinFile("notes.txt"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AGeneration2ProfileManifest_MigratesWithEveryMember()
        {
            var service = new SerializationService();
            var manifest = new ProfileManifest
            {
                GameVersion = "1.2.0",
                ModelGeneration = ModelGenerations.V2_SimplifyEntrance,
                Platform = "Windows",
                Categories = ProfileCategory.Levels | ProfileCategory.Settings,
            };
            manifest.Levels.Add(new ProfileLevelEntry());

            var root = JObject.Parse(service.SerializeData(manifest));
            root[Names.Generation] = ModelGenerations.V2_SimplifyEntrance;

            var migrated = service.DeserializeData<ProfileManifest>(root.ToString());

            Assert.AreEqual(manifest, migrated);
            Assert.AreEqual(ProfileCategory.Skins, ProfileLayout.CategoryOf("skins/a.json"));
            Assert.AreEqual(FileNames.SkinsDirectory, ProfileLayout.RootOf(ProfileCategory.Skins));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ASkinAboveItsGeneration_IsRefused()
        {
            var service = new SerializationService();
            var root = JObject.Parse(service.SerializeData(new AvatarSkin()));
            root[Names.Generation] = MockData.FabricatedGeneration;

            var refused = Assert.Throws<NewerGenerationException>(() =>
                service.DeserializeData<AvatarSkin>(root.ToString()));

            Assert.AreEqual(ModelDomains.AvatarSkin, refused.Domain);
        }
    }
}