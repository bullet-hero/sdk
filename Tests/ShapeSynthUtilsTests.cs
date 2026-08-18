using BH.SDK.Models.Data;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;
using BH.SDK.Utils;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // ShapeSynthUtils builds the shapes the game does not ship as presets. Its whole claim is that
    // a caller can write plain trigonometry and get back geometry the FORMAT accepts - so what is
    // asserted here is exactly that claim, not the trigonometry: every generator's output must be
    // inside the authored box, within both caps, and already sanitized (running Sanitize again must
    // find nothing left to change).
    public class ShapeSynthUtilsTests
    {
        private static ShapeId Id => ShapeId.NewGuid();

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Polygon_EverySideCount_IsLegalGeometry()
        {
            for (var sides = ShapeSynthUtils.MinSides; sides <= 32; sides++)
                AssertLegal(ShapeSynthUtils.Polygon(Id, $"Polygon{sides}", sides));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Wedge_EveryFraction_IsLegalGeometry()
        {
            foreach (var turns in new[] { 0.125f, 0.25f, 0.5f, 0.75f, 1f })
                AssertLegal(ShapeSynthUtils.Wedge(Id, $"Wedge{turns}", 24, turns));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Ring_EveryThickness_IsLegalGeometry()
        {
            foreach (var thickness in new[] { 0.05f, 0.25f, 0.5f, 0.9f })
                AssertLegal(ShapeSynthUtils.Ring(Id, $"Ring{thickness}", 16, thickness));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void RingWedge_TheCombinationNoPresetCovers_IsLegalGeometry()
        {
            foreach (var turns in new[] { 0.125f, 0.25f, 0.5f })
                AssertLegal(ShapeSynthUtils.RingWedge(Id, $"RingWedge{turns}", 24, 0.25f, turns));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Arrows_AreLegalGeometry()
        {
            AssertLegal(ShapeSynthUtils.Arrow(Id, "Arrow"));
            AssertLegal(ShapeSynthUtils.ArrowHead(Id, "ArrowHead"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Rect_IsLegalGeometry()
        {
            AssertLegal(ShapeSynthUtils.Rect(Id, "Rect"));
            AssertLegal(ShapeSynthUtils.Rect(Id, "Thin", 0.2f, 1f));
        }

        // The generators take degrees-of-freedom a caller can get wrong; nothing here may throw or
        // return something illegal because of it.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void OutOfRangeParameters_AreClampedRatherThanTrusted()
        {
            AssertLegal(ShapeSynthUtils.Polygon(Id, "TooFew", 1));
            AssertLegal(ShapeSynthUtils.Polygon(Id, "TooMany", 10_000));
            AssertLegal(ShapeSynthUtils.RingWedge(Id, "Degenerate", 3, 0f, 0f));
            AssertLegal(ShapeSynthUtils.RingWedge(Id, "Solid", 64, 1f, 2f));
            AssertLegal(ShapeSynthUtils.Arrow(Id, "Flat", 0f, 0f, 0f));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Build_KeepsTheIdAndNameItWasGiven()
        {
            var id = ShapeId.NewGuid();
            var shape = ShapeSynthUtils.Polygon(id, "Hexagon", 6);

            Assert.AreEqual(id, shape.ShapeId);
            Assert.AreEqual("Hexagon", shape.ShapeName);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Build_NothingSurvivingSanitize_ReturnsNullRatherThanAnEmptyShape()
        {
            // Three points on one line collapse to no triangle at all.
            var vertices = new System.Collections.Generic.List<Models.Values.Vector2Value>
            {
                new(-0.5f, 0f), new(0f, 0f), new(0.5f, 0f),
            };
            var indices = new System.Collections.Generic.List<int> { 0, 1, 2 };

            Assert.IsNull(ShapeSynthUtils.Build(ShapeId.NewGuid(), "Degenerate", vertices, indices));
        }

        private static void AssertLegal(CompositeShape shape)
        {
            Assert.IsNotNull(shape, "a generator returned nothing");

            Assert.GreaterOrEqual(shape.TriangleCount, ValueRules.MinShapeTriangles);
            Assert.LessOrEqual(shape.TriangleCount, ValueRules.MaxShapeTriangles);
            Assert.GreaterOrEqual(shape.Vertices.Count, ValueRules.MinShapeVertices);
            Assert.LessOrEqual(shape.Vertices.Count, ValueRules.MaxShapeVertices);

            foreach (var vertex in shape.Vertices)
            {
                Assert.GreaterOrEqual(vertex.X, ValueRules.MinShapePoint);
                Assert.LessOrEqual(vertex.X, ValueRules.MaxShapePoint);
                Assert.GreaterOrEqual(vertex.Y, ValueRules.MinShapePoint);
                Assert.LessOrEqual(vertex.Y, ValueRules.MaxShapePoint);
            }

            Assert.IsTrue(ShapeGeometryUtils.AreIndicesWellFormed(shape.Vertices, shape.Indices));
            Assert.AreEqual(0, ShapeGeometryUtils.CountBackFacing(shape.Vertices, shape.Indices),
                "every generator's winding must already be front-facing");

            // Idempotent by construction: the generator ran Sanitize, so a second pass has nothing
            // left to repair. If this fails, a generator is producing something it then relies on
            // Sanitize to quietly fix.
            var report = ShapeGeometryUtils.Sanitize(shape.Vertices, shape.Indices);
            Assert.AreEqual(0, report.DegenerateTriangles);
            Assert.AreEqual(0, report.FlippedTriangles);
            Assert.AreEqual(0, report.OrphanVertices);
            Assert.AreEqual(0, report.MalformedIndices);
        }
    }
}
