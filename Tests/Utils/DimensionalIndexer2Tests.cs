using System.Collections.Generic;
using System.Linq;
using BH.SDK.Utils;
using NUnit.Framework;

namespace BH.SDK.Tests.Utils
{
    // FLATTENING A GRID IS ONE MULTIPLICATION AND ONE ADDITION, AND THIS TYPE SPELLS IT SEVEN TIMES.
    // GetIndex and GetIndexes each exist as an instance method, a static method taking the width, a
    // tuple-taking overload and an out-parameter one - so the defect available here is not "the
    // arithmetic is wrong" but "two of the seven disagree", which is invisible until one caller
    // reads a cell another wrote.
    //
    // So the test that matters is the ROUND TRIP over every cell of a non-square grid, checked
    // through every overload at once: width and height are different numbers on purpose, because a
    // square grid makes a transposed formula indistinguishable from a correct one, and it is
    // precisely the row-major-versus-column-major slip that a hand-written index invites.
    //
    // The enumerators are checked against the same arithmetic rather than against a hand-written
    // sequence, for the same reason: a list of expected pairs is a second implementation, and a test
    // that restates the thing it checks passes whenever both copies are wrong the same way.

    /// <summary> DimensionalIndexer2: every overload flattens and unflattens the same way, and the
    /// enumerators visit every cell exactly once in row-major order. </summary>
    public class DimensionalIndexer2Tests
    {
        // Different, and neither of them 1 - a square grid hides a transposed formula, and a grid one
        // cell wide hides everything.
        private const int Width = 4;
        private const int Height = 3;

        private static DimensionalIndexer2 Grid() => new(Width, Height);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Length_IsWidthTimesHeight()
        {
            var grid = Grid();

            Assert.AreEqual(Width, grid.LengthWidth);
            Assert.AreEqual(Height, grid.LengthHeight);
            Assert.AreEqual(Width * Height, grid.Length);
        }

        // Row-major, stated as a fact rather than derived: the first row is the first `Width`
        // indices. Everything else in the type follows from this one sentence.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void GetIndex_IsRowMajor()
        {
            var grid = Grid();

            Assert.AreEqual(0, grid.GetIndex(0, 0));
            Assert.AreEqual(1, grid.GetIndex(1, 0), "the second cell is the next COLUMN, not the next row");
            Assert.AreEqual(Width, grid.GetIndex(0, 1), "the second row starts one width in");
            Assert.AreEqual(Width * Height - 1, grid.GetIndex(Width - 1, Height - 1));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void EveryCell_RoundTripsThroughEveryOverload()
        {
            var grid = Grid();

            for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
            {
                var expected = grid.GetIndex(x, y);
                var where = $"({x}, {y})";

                Assert.AreEqual(expected, grid.GetIndex(new DimensionalIndexer2.IndexTuple(x, y)),
                    $"the tuple overload disagrees at {where}");
                Assert.AreEqual(expected, DimensionalIndexer2.GetIndex(x, y, Width),
                    $"the static overload disagrees at {where}");
                Assert.AreEqual(expected,
                    DimensionalIndexer2.GetIndex(new DimensionalIndexer2.IndexTuple(x, y), Width),
                    $"the static tuple overload disagrees at {where}");

                var (backX, backY) = grid.GetIndexes(expected);
                Assert.AreEqual(x, backX, $"unflattening {expected} gave the wrong column");
                Assert.AreEqual(y, backY, $"unflattening {expected} gave the wrong row");

                grid.GetIndexes(expected, out var outX, out var outY);
                Assert.AreEqual(x, outX, $"the out overload disagrees at {where}");
                Assert.AreEqual(y, outY, $"the out overload disagrees at {where}");

                var (staticX, staticY) = DimensionalIndexer2.GetIndexes(expected, Width);
                Assert.AreEqual(x, staticX, $"the static unflatten disagrees at {where}");
                Assert.AreEqual(y, staticY, $"the static unflatten disagrees at {where}");

                DimensionalIndexer2.GetIndexes(expected, Width, out var staticOutX, out var staticOutY);
                Assert.AreEqual(x, staticOutX, $"the static out unflatten disagrees at {where}");
                Assert.AreEqual(y, staticOutY, $"the static out unflatten disagrees at {where}");
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Enumerate_VisitsEveryFlatIndexInOrder()
        {
            var grid = Grid();
            var visited = new List<int>();

            var enumerator = grid.Enumerate();
            while (enumerator.MoveNext()) visited.Add(enumerator.Current);

            CollectionAssert.AreEqual(Enumerable.Range(0, Width * Height), visited);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Enumerate2_VisitsEveryCellOnceInTheSameOrderGetIndexImplies()
        {
            var grid = Grid();
            var visited = grid.Enumerate2().ToList();

            Assert.AreEqual(Width * Height, visited.Count, "a cell was visited twice or not at all");

            for (var i = 0; i < visited.Count; i++)
            {
                var (x, y) = visited[i];
                Assert.AreEqual(i, grid.GetIndex(x, y),
                    $"the {i}th cell enumerated is ({x}, {y}), which flattens to {grid.GetIndex(x, y)}");
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void StaticEnumerators_AgreeWithTheInstanceOnes()
        {
            var grid = Grid();

            var instance = new List<int>();
            var instanceEnumerator = grid.Enumerate();
            while (instanceEnumerator.MoveNext()) instance.Add(instanceEnumerator.Current);

            var statics = new List<int>();
            var staticEnumerator = DimensionalIndexer2.Enumerate(Width, Height);
            while (staticEnumerator.MoveNext()) statics.Add(staticEnumerator.Current);

            CollectionAssert.AreEqual(instance, statics);
            CollectionAssert.AreEqual(grid.Enumerate2().ToList(),
                DimensionalIndexer2.Enumerate2(Width, Height).ToList());
        }

        // Pairing a collection with coordinates is what draws a grid of anything, and it has to wrap
        // at the WIDTH - wrapping at the height instead is the same transposition mistake, one layer
        // up, and produces a grid that looks plausible until it is not square.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Enumerate2_OverACollection_WrapsAtTheWidth()
        {
            var grid = Grid();
            var items = Enumerable.Range(0, Width * Height).ToList();

            var paired = grid.Enumerate2(items).ToList();

            Assert.AreEqual(items.Count, paired.Count);

            foreach (var (item, x, y) in paired)
            {
                Assert.AreEqual(item, grid.GetIndex(x, y),
                    $"item {item} was placed at ({x}, {y}), which is index {grid.GetIndex(x, y)}");
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Enumerate_OverACollection_CountsFromZero()
        {
            var items = new[] { "a", "b", "c" };

            var paired = DimensionalIndexer2.Enumerate(items).ToList();

            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, paired.Select(pair => pair.Item2).ToList());
            CollectionAssert.AreEqual(items, paired.Select(pair => pair.Item1).ToList());
        }

        // A collection shorter than the grid is the ordinary case for a picker - the last row is
        // partly empty - and it must simply stop rather than pad or wrap round.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Enumerate2_OverAShortCollection_StopsWithIt()
        {
            var grid = Grid();

            var paired = grid.Enumerate2(new[] { 0, 1, 2, 3, 4 }).ToList();

            Assert.AreEqual(5, paired.Count);
            Assert.AreEqual((4, 0, 1), paired[4], "the fifth item starts the second row of a 4-wide grid");
        }
    }
}
