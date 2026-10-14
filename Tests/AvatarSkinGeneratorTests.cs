using System.Linq;
using BH.SDK.Models;
using BH.SDK.Services.Skins;
using BH.SDK.Utils;
using BH.SDK.Validations;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    /// <summary> That every generated preset is drawable, symmetric, repeatable and distinct. </summary>
    [TestFixture]
    public class AvatarSkinGeneratorTests
    {
        private const int SeedCount = 500;

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void EveryPreset_ValidatesAndIsMirrorSymmetric()
        {
            for (var seed = 0u; seed < SeedCount; seed++)
            {
                var skin = AvatarSkinGenerator.Generate(seed);

                CollectionAssert.IsEmpty(AvatarSkinValidation.Validate(skin), $"seed {seed}");
                Assert.IsTrue(skin.Id.IsBuiltin(), $"seed {seed}: a preset id is never a file's");
                for (var row = 0; row < skin.Rows; row++)
                for (var column = 0; column < skin.Columns; column++)
                    Assert.AreEqual(AvatarSkinMask.Has(skin.Mask, column, row),
                        AvatarSkinMask.Has(skin.Mask, skin.Columns - 1 - column, row), $"seed {seed}: row {row}");
            }
        }

        // Rows top first, '#' filled - only a way to write a mask readably in a test.
        private static bool[,] Grid(params string[] rows)
        {
            var grid = new bool[rows.Length, rows[0].Length];
            for (var row = 0; row < rows.Length; row++)
            for (var column = 0; column < rows[0].Length; column++)
                grid[row, column] = rows[row][column] == '#';
            return grid;
        }

        private static bool Face(params string[] rows) =>
            AvatarSkinGenerator.LooksLikeAFace(Grid(rows), rows.Length, rows[0].Length);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void EyesAndMouths_ReadAsAFace_ARingDoesNot()
        {
            Assert.IsTrue(Face("#####", "#.#.#", "#####", "#####", "#####"), "two holes are eyes");
            Assert.IsTrue(Face("#####", "#####", "#####", "#...#", "#####"), "a wide slot is a mouth");
            Assert.IsTrue(Face("#######", "##.#.##", "##...##", "#######", "#######"), "a crooked hole is a smile");
            Assert.IsTrue(Face("#####", "##.##", "#####", "#####", "#####"), "a hole off the middle row");

            Assert.IsFalse(Face("#####", "#####", "#####", "#####", "#####"));
            Assert.IsFalse(Face("####", "#..#", "#..#", "####"), "a centred box is a ring");
            Assert.IsFalse(Face("#####", "#####", "##.##", "#####", "#####"));
            Assert.IsFalse(Face(".#.#.", "#####", "#####", "#####", ".#.#."), "notches open to the edge are no holes");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void NoPreset_HasAFace()
        {
            for (var seed = 0u; seed < SeedCount; seed++)
            {
                var skin = AvatarSkinGenerator.Generate(seed);
                var grid = new bool[skin.Rows, skin.Columns];
                for (var row = 0; row < skin.Rows; row++)
                for (var column = 0; column < skin.Columns; column++)
                    grid[skin.Rows - 1 - row, column] = AvatarSkinMask.Has(skin.Mask, column, row);

                Assert.IsFalse(AvatarSkinGenerator.LooksLikeAFace(grid, skin.Rows, skin.Columns), $"seed {seed}");
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TheSameSeed_GivesTheSamePreset()
        {
            for (var seed = 0u; seed < 20; seed++)
                Assert.AreEqual(AvatarSkinGenerator.Generate(seed), AvatarSkinGenerator.Generate(seed), $"seed {seed}");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void EveryStyleShape_IsACatalogueShape()
        {
            for (var seed = 0u; seed < SeedCount; seed++)
                foreach (var part in AvatarSkinGenerator.Generate(seed).Parts)
                    Assert.IsTrue(AvatarSkinShapes.IsCatalogue(part.Shape), $"seed {seed}: {part.Shape.value}");

            var styles = Enumerable.Range(0, SeedCount)
                .Select(seed => AvatarSkinGenerator.Generate((uint)seed).Name.Split(' ')[0])
                .Distinct().ToList();
            CollectionAssert.AreEquivalent(AvatarSkinGenerator.Styles, styles, "every style is drawn");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void GenerateMany_SkipsDuplicateGeometry()
        {
            var skins = AvatarSkinGenerator.GenerateMany(SeedCount, 0u);

            Assert.AreEqual(SeedCount, skins.Count);
            var keys = skins.Select(AvatarSkinGenerator.GeometryKey).ToList();
            Assert.AreEqual(keys.Count, keys.Distinct().Count());
        }
    }
}
