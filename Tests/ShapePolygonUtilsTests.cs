using System.Collections.Generic;
using System.Linq;
using BH.SDK.Models.Values;
using BH.SDK.Utils;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    /// <summary> ShapePolygonUtils: an outline fitted to the authored box and cut into front-facing
    /// triangles. </summary>
    public class ShapePolygonUtilsTests
    {
        private static List<Vector2Value> Outline(params float[] xy)
        {
            var points = new List<Vector2Value>();
            for (var i = 0; i + 1 < xy.Length; i += 2) points.Add(new Vector2Value(xy[i], xy[i + 1]));
            return points;
        }

        private static float Area(IReadOnlyList<Vector2Value> points, List<int> indices)
        {
            var area = 0f;
            for (var i = 0; i < indices.Count; i += 3)
                area += ShapeGeometryUtils.SignedDoubleArea(points[indices[i]], points[indices[i + 1]],
                    points[indices[i + 2]]) * 0.5f;
            return area;
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void FitToBox_MapsTheBoundsOntoTheAuthoredBox_AndReportsThem()
        {
            var fitted = ShapePolygonUtils.FitToBox(Outline(2, 1, 6, 1, 6, 3, 2, 3), out var cx, out var cy,
                out var width, out var height);

            Assert.AreEqual(4f, cx, 1e-5f);
            Assert.AreEqual(2f, cy, 1e-5f);
            Assert.AreEqual(4f, width, 1e-5f);
            Assert.AreEqual(2f, height, 1e-5f);
            Assert.AreEqual(-0.5f, fitted[0].X, 1e-5f);
            Assert.AreEqual(-0.5f, fitted[0].Y, 1e-5f);
            Assert.AreEqual(0.5f, fitted[2].X, 1e-5f);
            Assert.AreEqual(0.5f, fitted[2].Y, 1e-5f);
        }

        [TestCase(true)]
        [TestCase(false)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TryTriangulate_CoversAConcaveOutline_FrontFacing_InEitherWinding(bool clockwise)
        {
            // An L: area 3 of a 2x2 box, one reflex corner.
            var points = Outline(0, 0, 2, 0, 2, 1, 1, 1, 1, 2, 0, 2);
            if (clockwise) points.Reverse();

            Assert.IsTrue(ShapePolygonUtils.TryTriangulate(points, out var indices));

            Assert.AreEqual((points.Count - 2) * 3, indices.Count);
            Assert.AreEqual(3f, Area(points, indices), 1e-4f);
            for (var i = 0; i < indices.Count; i += 3)
                Assert.Greater(ShapeGeometryUtils.SignedDoubleArea(points[indices[i]], points[indices[i + 1]],
                    points[indices[i + 2]]), 0f);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TryTriangulate_RefusesAnOutlineWithNoArea()
        {
            Assert.IsFalse(ShapePolygonUtils.TryTriangulate(Outline(0, 0, 1, 1, 2, 2), out var indices));
            Assert.IsEmpty(indices);
            Assert.IsFalse(ShapePolygonUtils.TryTriangulate(Outline(0, 0, 1, 1), out _));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TryTriangulate_RefusesAnOutlineThatCrossesItself()
        {
            // A bow tie: the two edges through the middle cross.
            Assert.IsFalse(ShapePolygonUtils.TryTriangulate(Outline(0, 0, 2, 2, 2, 0, 0, 2), out _));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TryTriangulate_ARegularPolygon_UsesEveryCorner()
        {
            var points = Enumerable.Range(0, 12)
                .Select(i => new Vector2Value((float)System.Math.Cos(i * System.Math.PI / 6),
                    (float)System.Math.Sin(i * System.Math.PI / 6)))
                .ToList();

            Assert.IsTrue(ShapePolygonUtils.TryTriangulate(points, out var indices));
            CollectionAssert.AreEquivalent(Enumerable.Range(0, 12), indices.Distinct());
        }
    }
}
