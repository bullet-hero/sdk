using System.Collections.Generic;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Services.Shapes;
using BH.SDK.Utils;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // The repair pipeline is order-dependent and every step can undo an earlier one, so these test
    // the ORDER as much as the individual passes: clamping can collapse a triangle, welding can
    // collapse another, dropping triangles can orphan points, and winding has to run after all of
    // it. A test that only checked each pass in isolation would pass while Sanitize corrupted data.

    /// <summary>
    /// ShapeGeometryUtils: the shared "what is a valid shape" implementation behind both
    /// RuleShapeGeometry.Fix and the in-game shape editor's Save.
    /// </summary>
    public class ShapeGeometryUtilsTests
    {
        private static List<Vector2Value> SquareVertices() => new()
        {
            new Vector2Value(-0.5f, -0.5f),
            new Vector2Value(0.5f, -0.5f),
            new Vector2Value(0.5f, 0.5f),
            new Vector2Value(-0.5f, 0.5f),
        };

        private static List<int> SquareIndices() => new() { 0, 1, 2, 0, 2, 3 };

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void SignedDoubleArea_IsPositiveForFrontFacing()
        {
            var a = new Vector2Value(-0.5f, -0.5f);
            var b = new Vector2Value(0.5f, -0.5f);
            var c = new Vector2Value(0.5f, 0.5f);

            Assert.Greater(ShapeGeometryUtils.SignedDoubleArea(a, b, c), 0f);
            Assert.Less(ShapeGeometryUtils.SignedDoubleArea(a, c, b), 0f);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void IsDegenerate_DetectsCollinear()
        {
            var a = new Vector2Value(-0.5f, 0f);
            var b = new Vector2Value(0f, 0f);
            var c = new Vector2Value(0.5f, 0f);

            Assert.IsTrue(ShapeGeometryUtils.IsDegenerate(a, b, c));
            Assert.IsFalse(ShapeGeometryUtils.IsDegenerate(a, b, new Vector2Value(0f, 0.5f)));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Analyze_ValidSquare_IsClean()
        {
            var report = ShapeGeometryUtils.Analyze(SquareVertices(), SquareIndices());

            Assert.IsTrue(report.IsClean, report.Describe());
            Assert.AreEqual(string.Empty, report.Describe());
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Sanitize_ValidSquare_ChangesNothing()
        {
            var vertices = SquareVertices();
            var indices = SquareIndices();

            var report = ShapeGeometryUtils.Sanitize(vertices, indices);

            Assert.IsTrue(report.IsClean, report.Describe());
            Assert.AreEqual(4, vertices.Count);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 0, 2, 3 }, indices);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void FixWinding_FlipsOnlyBackFacing()
        {
            var vertices = SquareVertices();
            var indices = new List<int> { 0, 2, 1, 0, 2, 3 };

            var fixedCount = ShapeGeometryUtils.FixWinding(vertices, indices);

            Assert.AreEqual(1, fixedCount);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 0, 2, 3 }, indices);
            Assert.AreEqual(0, ShapeGeometryUtils.CountBackFacing(vertices, indices));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void WeldPoints_MergesDuplicatesAndRemapsIndices()
        {
            var vertices = SquareVertices();
            vertices.Add(new Vector2Value(-0.5f, -0.5f));
            var indices = new List<int> { 0, 1, 2, 4, 2, 3 };

            var removed = ShapeGeometryUtils.WeldPoints(vertices, indices);

            Assert.AreEqual(1, removed);
            Assert.AreEqual(4, vertices.Count);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 0, 2, 3 }, indices);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void DropOrphanVertices_CompactsAndRemaps()
        {
            var vertices = SquareVertices();
            vertices.Insert(0, new Vector2Value(0.1f, 0.1f));
            var indices = new List<int> { 1, 2, 3, 1, 3, 4 };

            var removed = ShapeGeometryUtils.DropOrphanVertices(vertices, indices);

            Assert.AreEqual(1, removed);
            Assert.AreEqual(4, vertices.Count);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 0, 2, 3 }, indices);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void DropMalformedTriples_KeepsOnlyResolvableTriangles()
        {
            var vertices = SquareVertices();
            var indices = new List<int> { 0, 1, 2, 0, 2, 99, 0 };

            var dropped = ShapeGeometryUtils.DropMalformedTriples(vertices, indices);

            Assert.AreEqual(2, dropped, "one out-of-range triple plus the trailing partial one");
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, indices);
        }

        // The point of the ordering: clamping moves a corner onto its neighbour, welding then merges
        // them, and only then is the triangle collapsed - a pipeline that clamped last would store a
        // triangle with two identical corners and report itself clean.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Sanitize_ClampCollapsesTriangle_RemovesItAndItsPoints()
        {
            var vertices = new List<Vector2Value>
            {
                new(-0.5f, -0.5f),
                new(0.8f, -0.5f),
                new(0.9f, -0.5f),
            };
            var indices = new List<int> { 0, 1, 2 };

            var report = ShapeGeometryUtils.Sanitize(vertices, indices);

            Assert.AreEqual(2, report.OutOfBoundsPoints);
            Assert.AreEqual(1, report.WeldedVertices);
            Assert.AreEqual(1, report.DegenerateTriangles);
            CollectionAssert.IsEmpty(indices);
            CollectionAssert.IsEmpty(vertices);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Sanitize_RepairsEverythingAtOnce()
        {
            var vertices = SquareVertices();
            vertices.Add(new Vector2Value(0.9f, 0.9f));   // out of bounds, becomes a duplicate of [2]
            vertices.Add(new Vector2Value(0.2f, 0.2f));   // orphan
            var indices = new List<int>
            {
                0, 2, 1,        // back-facing
                0, 4, 3,        // uses the out-of-bounds point
            };

            var report = ShapeGeometryUtils.Sanitize(vertices, indices);

            Assert.AreEqual(1, report.OutOfBoundsPoints);
            Assert.AreEqual(1, report.WeldedVertices);
            Assert.AreEqual(1, report.OrphanVertices);
            Assert.AreEqual(1, report.FlippedTriangles);
            Assert.AreEqual(0, ShapeGeometryUtils.CountBackFacing(vertices, indices));
            Assert.AreEqual(2, ShapeGeometryUtils.GetTriangleCount(indices));
            Assert.AreEqual(4, vertices.Count);
            Assert.IsTrue(ShapeGeometryUtils.Analyze(vertices, indices).IsClean);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Sanitize_IsIdempotent()
        {
            var vertices = SquareVertices();
            vertices.Add(new Vector2Value(2f, 2f));
            var indices = new List<int> { 0, 2, 1, 0, 2, 3 };

            ShapeGeometryUtils.Sanitize(vertices, indices);
            var second = ShapeGeometryUtils.Sanitize(vertices, indices);

            Assert.IsTrue(second.IsClean, second.Describe());
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Sanitize_OverTriangleCap_TrimsAndKeepsGeometryConsistent()
        {
            var vertices = new List<Vector2Value>();
            var indices = new List<int>();
            var count = ValueRules.MaxShapeTriangles + 5;

            // Distinct, non-degenerate, front-facing triangles - stacked in a thin column so they
            // never share a weld cell.
            for (var i = 0; i < count; i++)
            {
                var y = -0.5f + i * 0.001f;
                var index = vertices.Count;
                vertices.Add(new Vector2Value(-0.4f, y));
                vertices.Add(new Vector2Value(-0.3f, y));
                vertices.Add(new Vector2Value(-0.35f, y + 0.0005f));
                indices.Add(index);
                indices.Add(index + 1);
                indices.Add(index + 2);
            }

            var report = ShapeGeometryUtils.Sanitize(vertices, indices);

            Assert.AreEqual(5, report.ExcessTriangles);
            Assert.AreEqual(ValueRules.MaxShapeTriangles, ShapeGeometryUtils.GetTriangleCount(indices));
            Assert.LessOrEqual(vertices.Count, ValueRules.MaxShapeVertices);
            Assert.IsTrue(ShapeGeometryUtils.Analyze(vertices, indices).IsClean);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Sanitize_NullVertex_IsReplacedNotRemoved()
        {
            var vertices = SquareVertices();
            vertices[1] = null;
            var indices = SquareIndices();

            var report = ShapeGeometryUtils.Sanitize(vertices, indices);

            Assert.AreEqual(1, report.NullVertices);
            // The second triangle never referenced the null point and must survive untouched - a
            // repair that removed the point instead would have shifted its indices.
            Assert.AreEqual(1, ShapeGeometryUtils.GetTriangleCount(indices));
            Assert.IsTrue(ShapeGeometryUtils.Analyze(vertices, indices).IsClean);
        }

        // GetCentroidPivot IS THE GENERAL FORM OF ShapeCatalogService's per-form one, and the test
        // that matters is that the two agree: the six alignment constants an author already clicks
        // were computed from a form's rim, and a triangle soup has no rim to read. Weighting by
        // area is what makes them agree - a corner average is the centre of mass only for a
        // triangle and a regular polygon, which is every case the per-form version ever saw.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void GetCentroidPivot_IsTheBoxCentreForASquare()
        {
            var pivot = ShapeGeometryUtils.GetCentroidPivot(SquareVertices(), SquareIndices());

            Assert.AreEqual(0.5f, pivot.X, 1e-5f);
            Assert.AreEqual(0.5f, pivot.Y, 1e-5f);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void GetCentroidPivot_IsTheBoxCentreWhenNothingHasArea()
        {
            var empty = ShapeGeometryUtils.GetCentroidPivot(new List<Vector2Value>(), new List<int>());

            Assert.AreEqual(0.5f, empty.X, 1e-5f);
            Assert.AreEqual(0.5f, empty.Y, 1e-5f);

            // Collinear points describe no area, so there is nothing to balance and the answer has
            // to be the same one rather than a division by zero.
            var collinear = ShapeGeometryUtils.GetCentroidPivot(
                new List<Vector2Value>
                {
                    new(-0.5f, 0f), new(0f, 0f), new(0.5f, 0f),
                },
                new List<int> { 0, 1, 2 });

            Assert.AreEqual(0.5f, collinear.X, 1e-5f);
            Assert.AreEqual(0.5f, collinear.Y, 1e-5f);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void GetCentroidPivot_WeighsEachTriangleByItsArea()
        {
            // A big triangle on the left and a tiny one on the right. Averaging the six corners
            // pulls the answer far right; the centre of mass barely moves.
            var vertices = new List<Vector2Value>
            {
                new(-0.5f, -0.5f), new(0f, -0.5f), new(-0.5f, 0.5f),
                new(0.4f, -0.5f), new(0.5f, -0.5f), new(0.4f, -0.4f),
            };
            var indices = new List<int> { 0, 1, 2, 3, 4, 5 };

            var pivot = ShapeGeometryUtils.GetCentroidPivot(vertices, indices);

            const float bigArea = 0.5f * 0.5f * 1.0f;
            const float smallArea = 0.5f * 0.1f * 0.1f;
            var bigX = (-0.5f + 0f + -0.5f) / 3f;
            var smallX = (0.4f + 0.5f + 0.4f) / 3f;
            var expected = 0.5f + (bigArea * bigX + smallArea * smallX) / (bigArea + smallArea);

            Assert.AreEqual(expected, pivot.X, 1e-4f);

            // The corner average of the same six points lands at 0.55 - right of the box centre,
            // outside the big triangle entirely. That is the answer this must not give.
            Assert.Less(pivot.X, 0.3f);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void GetCentroidPivot_MatchesThePerFormAnswerForEveryBuiltInForm()
        {
            foreach (var form in ShapeCatalogService.EnumerateForms())
            {
                var expected = ShapeCatalogService.GetCentroidPivot(form);
                var shape = ShapeCatalogService.Build(new ShapeParameters(form));

                var actual = ShapeGeometryUtils.GetCentroidPivot(shape);

                Assert.AreEqual(expected.X, actual.X, 1e-4f, $"{form.Name} X");
                Assert.AreEqual(expected.Y, actual.Y, 1e-4f, $"{form.Name} Y");
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Describe_NamesEveryFinding()
        {
            var report = new ShapeGeometryReport { FlippedTriangles = 2, OrphanVertices = 1 };

            var text = report.Describe();

            StringAssert.Contains("2 triangle winding fixed", text);
            StringAssert.Contains("1 unconnected points removed", text);
        }
    }
}
