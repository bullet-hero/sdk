using System.Linq;
using BH.SDK.Generators.Geometry;
using BH.SDK.Models;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using NUnit.Framework;

namespace BH.SDK.Tests.Generators
{
    // GeneratorSweepTests already proves the properties every generator shares. What is left per
    // generator is its actual geometry - the part a sweep cannot check, and the part an author
    // notices immediately when it is wrong.
    public class GeometryGeneratorsTests
    {
        private static Level CreateLevel()
        {
            var level = new Level();
            level.Settings.Fps = 60;
            level.Settings.FrameDuration = 600;
            return level;
        }

        private static BH.SDK.Generators.GeneratorContext Context(Level level) => new(level, FrameSpan.FromBounds(0, 120));

        private static Vector2Value PositionOf(RectObject obj) => (Vector2Value)obj.Positions[0].Pos;

        private static float DistanceFromOrigin(RectObject obj)
        {
            var position = PositionOf(obj);
            return (float)System.Math.Sqrt(position.X * position.X + position.Y * position.Y);
        }

        #region Grid

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Grid_PlacesColumnsTimesRows()
        {
            var level = CreateLevel();
            new GridGenerator().Run(Context(level), new GridGenerator.Parameters
            {
                Columns = 5, Rows = 3, Centered = false,
            });

            Assert.AreEqual(15, level.Game.Objects.Count);
        }

        // Centering a 2x1 grid must put its cells at -spacing/2 and +spacing/2 - the classic
        // off-by-one here centres on a cell instead of between cells.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Grid_CentersOnTheSpanNotTheCellCount()
        {
            var level = CreateLevel();
            new GridGenerator().Run(Context(level), new GridGenerator.Parameters
            {
                Columns = 2, Rows = 1, SpacingX = 4f, Centered = true,
            });

            var xs = level.Game.Objects.Values.Select(obj => PositionOf(obj).X).OrderBy(x => x).ToList();
            Assert.AreEqual(2, xs.Count);
            Assert.AreEqual(-2f, xs[0], 0.001f);
            Assert.AreEqual(2f, xs[1], 0.001f);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Grid_UncenteredStartsAtTheOrigin()
        {
            var level = CreateLevel();
            new GridGenerator().Run(Context(level), new GridGenerator.Parameters
            {
                Columns = 2, Rows = 2, SpacingX = 3f, SpacingY = 3f,
                OriginX = 10f, OriginY = -5f, Centered = false,
            });

            Assert.IsTrue(level.Game.Objects.Values.Any(obj =>
                System.Math.Abs(PositionOf(obj).X - 10f) < 0.001f &&
                System.Math.Abs(PositionOf(obj).Y + 5f) < 0.001f));
        }

        #endregion

        #region Radial

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Radial_PlacesEveryObjectOnTheCircle()
        {
            var level = CreateLevel();
            new RadialGenerator().Run(Context(level), new RadialGenerator.Parameters
            {
                Count = 8, Radius = 5f,
            });

            Assert.AreEqual(8, level.Game.Objects.Count);
            foreach (var obj in level.Game.Objects.Values)
                Assert.AreEqual(5f, DistanceFromOrigin(obj), 0.001f);
        }

        // A full circle must not put two objects on the same spot at 0 and 360 degrees; a partial
        // arc must keep BOTH of its ends, which is the opposite convention.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Radial_FullCircleDropsTheDuplicateEnd_PartialArcKeepsBothEnds()
        {
            var full = CreateLevel();
            new RadialGenerator().Run(Context(full), new RadialGenerator.Parameters
            {
                Count = 4, Radius = 1f, Arc = 360f, StartAngle = 0f,
            });
            var fullAngles = full.Game.Objects.Values.Select(AngleOf).OrderBy(a => a).ToList();
            CollectionAssert.AllItemsAreUnique(fullAngles);
            Assert.AreEqual(0f, fullAngles[0], 0.01f);
            Assert.AreEqual(270f, fullAngles[3], 0.01f);

            var partial = CreateLevel();
            new RadialGenerator().Run(Context(partial), new RadialGenerator.Parameters
            {
                Count = 3, Radius = 1f, Arc = 90f, StartAngle = 0f,
            });
            var partialAngles = partial.Game.Objects.Values.Select(AngleOf).OrderBy(a => a).ToList();
            Assert.AreEqual(0f, partialAngles[0], 0.01f);
            Assert.AreEqual(45f, partialAngles[1], 0.01f);
            Assert.AreEqual(90f, partialAngles[2], 0.01f);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Radial_FaceCenterAddsRotationsPointingInward()
        {
            var level = CreateLevel();
            new RadialGenerator().Run(Context(level), new RadialGenerator.Parameters
            {
                Count = 4, Radius = 2f, Arc = 360f, StartAngle = 0f, FaceCenter = true,
            });

            foreach (var obj in level.Game.Objects.Values)
                Assert.AreEqual(1, obj.Rotations.Count);

            var first = level.Game.Objects.Values.First(obj => AngleOf(obj) < 0.01f);
            // Stored as RADIANS - the unit the format itself uses (see BaseSpawnGenerator).
            Assert.AreEqual((float)System.Math.PI, ((FloatValue)first.Rotations[0].Angle).Value, 0.01f);
        }

        private static float AngleOf(RectObject obj)
        {
            var position = PositionOf(obj);
            var degrees = (float)(System.Math.Atan2(position.Y, position.X) * (180.0 / System.Math.PI));
            return degrees < 0f ? degrees + 360f : degrees;
        }

        #endregion

        #region Spiral

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Spiral_RadiusGrowsFromStartToEnd()
        {
            var level = CreateLevel();
            new SpiralGenerator().Run(Context(level), new SpiralGenerator.Parameters
            {
                Count = 10, RadiusStart = 1f, RadiusEnd = 10f, Turns = 1f,
            });

            var radii = level.Game.Objects.OrderBy(pair => pair.Key.value)
                .Select(pair => DistanceFromOrigin(pair.Value)).ToList();

            Assert.AreEqual(1f, radii.First(), 0.001f);
            Assert.AreEqual(10f, radii.Last(), 0.001f);
            for (var i = 1; i < radii.Count; i++)
                Assert.Greater(radii[i], radii[i - 1], $"radius must grow monotonically at {i}");
        }

        #endregion

        #region Polygon

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Polygon_VertexModePlacesOnePerSide()
        {
            var level = CreateLevel();
            new PolygonGenerator().Run(Context(level), new PolygonGenerator.Parameters
            {
                Sides = 5, Radius = 3f, AsOutline = false,
            });

            Assert.AreEqual(5, level.Game.Objects.Count);
            foreach (var obj in level.Game.Objects.Values)
                Assert.AreEqual(3f, DistanceFromOrigin(obj), 0.001f);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Polygon_OutlineModePlacesSidesTimesPointsPerEdge_WithoutDoublingCorners()
        {
            var level = CreateLevel();
            new PolygonGenerator().Run(Context(level), new PolygonGenerator.Parameters
            {
                Sides = 4, Radius = 2f, AsOutline = true, PointsPerEdge = 3,
            });

            Assert.AreEqual(12, level.Game.Objects.Count);

            var positions = level.Game.Objects.Values.Select(PositionOf)
                .Select(p => $"{p.X:F3}:{p.Y:F3}").ToList();
            CollectionAssert.AllItemsAreUnique(positions);
        }

        // Sides below 3 is not a polygon; clamping rather than throwing keeps a half-typed value in
        // a form from taking the whole run down.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Polygon_ClampsDegenerateSideCount()
        {
            var level = CreateLevel();
            var generator = new PolygonGenerator();
            var parameters = new PolygonGenerator.Parameters { Sides = 1, Radius = 1f };

            var context = Context(level);
            var estimate = generator.Estimate(context, parameters);
            generator.Run(context, parameters);

            Assert.AreEqual(3, level.Game.Objects.Count);
            Assert.AreEqual(3, estimate.Objects);
        }

        #endregion

        #region Fractal

        [TestCase(FractalType.Koch, 0, 3)]
        [TestCase(FractalType.Koch, 1, 12)]
        [TestCase(FractalType.Koch, 2, 48)]
        [TestCase(FractalType.Sierpinski, 0, 1)]
        [TestCase(FractalType.Sierpinski, 3, 27)]
        [TestCase(FractalType.Tree, 0, 1)]
        [TestCase(FractalType.Tree, 3, 15)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Fractal_ObjectCountFollowsItsOwnFormula(FractalType type, int depth, int expected)
        {
            var level = CreateLevel();
            var generator = new FractalGenerator();
            var parameters = new FractalGenerator.Parameters { Type = type, Depth = depth, Scale = 5f };

            var context = Context(level);
            var estimate = generator.Estimate(context, parameters);
            generator.Run(context, parameters);

            Assert.AreEqual(expected, level.Game.Objects.Count, $"{type} depth {depth}");
            Assert.AreEqual(expected, estimate.Objects, $"{type} depth {depth} estimate");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Fractal_DepthIsCapped()
        {
            var level = CreateLevel();
            var generator = new FractalGenerator();
            var parameters = new FractalGenerator.Parameters
            {
                Type = FractalType.Sierpinski, Depth = 999, Scale = 5f,
            };

            var estimate = generator.Estimate(Context(level), parameters);

            Assert.AreEqual(729, estimate.Objects, "depth clamps to 6, so 3^6");
        }

        // Segment-shaped fractals size each object to its own segment, so the template Size must
        // have been replaced rather than appended to - two size keys on one frame would be invalid.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Fractal_SegmentsCarryExactlyOneSizeKey()
        {
            var level = CreateLevel();
            new FractalGenerator().Run(Context(level), new FractalGenerator.Parameters
            {
                Type = FractalType.Tree, Depth = 2, Scale = 4f,
            });

            foreach (var obj in level.Game.Objects.Values)
            {
                Assert.AreEqual(1, obj.Sizes.Count);
                Assert.AreEqual(1, obj.Rotations.Count);
            }
        }

        #endregion
    }
}
