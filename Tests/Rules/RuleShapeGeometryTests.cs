using System.Collections.Generic;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    // The second object-level rule, and the first one whose Fix rewrites collections rather than
    // swapping two values - so these also pin that RuleFixer can hand a whole object to a rule that
    // replaces the contents of two of its properties.

    /// <summary>
    /// RuleShapeGeometry: indexed shape geometry is well formed - whole triples, in-range indices,
    /// points inside the authored box, no degenerate or back-facing triangles, no orphan points.
    /// </summary>
    public class RuleShapeGeometryTests : BaseRuleTests
    {
        /// <summary> A model carrying real indexed geometry. </summary>
        [RuleContainer]
        [RuleShapeGeometry]
        private class GeometryModel : IShapeGeometry
        {
            public List<Vector2Value> Vertices { get; set; } = new();
            public List<int> Indices { get; set; } = new();
        }

        /// <summary> A model that does not implement the geometry contract at all. </summary>
        [RuleContainer]
        [RuleShapeGeometry]
        private class NotGeometryModel
        {
            public float Value { get; set; }
        }

        /// <summary> One counter-clockwise triangle well inside the box. </summary>
        private static GeometryModel Valid() => new()
        {
            Vertices = new List<Vector2Value>
            {
                new(-0.5f, -0.5f),
                new(0.5f, -0.5f),
                new(0.5f, 0.5f),
            },
            Indices = new List<int> { 0, 1, 2 },
        };

        /// <summary> A unit square as two triangles sharing a diagonal - four vertices, not six. </summary>
        private static GeometryModel ValidSquare() => new()
        {
            Vertices = new List<Vector2Value>
            {
                new(-0.5f, -0.5f),
                new(0.5f, -0.5f),
                new(0.5f, 0.5f),
                new(-0.5f, 0.5f),
            },
            Indices = new List<int> { 0, 1, 2, 0, 2, 3 },
        };

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Valid_SingleTriangle_Passes() => AssertValid(Valid());

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Valid_SharedCornerSquare_Passes() => AssertValid(ValidSquare());

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Empty_Passes()
        {
            // No geometry is not broken geometry - it is a shape nobody has drawn yet, and the
            // editor refuses to store it long before validation sees it.
            AssertValid(new GeometryModel());
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void BackFacingTriangle_IsInvalid()
        {
            var model = Valid();
            (model.Indices[1], model.Indices[2]) = (model.Indices[2], model.Indices[1]);

            AssertInvalid<RuleShapeGeometryAttribute>(model);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void BackFacingTriangle_FixRewinds()
        {
            var model = Valid();
            (model.Indices[1], model.Indices[2]) = (model.Indices[2], model.Indices[1]);

            AssertFixed(model);
            Assert.AreEqual(3, model.Indices.Count, "Fix must rewind the triangle, not drop it");
            Assert.AreEqual(3, model.Vertices.Count);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void PointOutsideBox_IsInvalidAndClamped()
        {
            var model = Valid();
            model.Vertices[2] = new Vector2Value(0.9f, 0.8f);

            AssertInvalid<RuleShapeGeometryAttribute>(model);
            AssertFixed(model);
            Assert.AreEqual(0.5f, model.Vertices[2].X, 1e-5f);
            Assert.AreEqual(0.5f, model.Vertices[2].Y, 1e-5f);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void OrphanVertex_IsInvalidAndRemoved()
        {
            var model = Valid();
            model.Vertices.Add(new Vector2Value(0.1f, 0.1f));

            AssertInvalid<RuleShapeGeometryAttribute>(model);
            AssertFixed(model);
            Assert.AreEqual(3, model.Vertices.Count);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, model.Indices);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void DegenerateTriangle_IsInvalidAndRemoved()
        {
            var model = new GeometryModel
            {
                Vertices = new List<Vector2Value>
                {
                    new(-0.5f, 0f),
                    new(0f, 0f),
                    new(0.5f, 0f),
                },
                Indices = new List<int> { 0, 1, 2 },
            };

            AssertInvalid<RuleShapeGeometryAttribute>(model);
            AssertFixed(model);
            CollectionAssert.IsEmpty(model.Indices);
            CollectionAssert.IsEmpty(model.Vertices, "A collapsed triangle leaves its corners orphaned");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void IndexOutOfRange_IsInvalidAndDropped()
        {
            var model = Valid();
            model.Indices.AddRange(new[] { 0, 1, 99 });

            AssertInvalid<RuleShapeGeometryAttribute>(model);
            AssertFixed(model);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, model.Indices);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void PartialTriple_IsInvalidAndDropped()
        {
            var model = Valid();
            model.Indices.Add(0);

            AssertInvalid<RuleShapeGeometryAttribute>(model);
            AssertFixed(model);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, model.Indices);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void DuplicatePoints_AreWeldedAndIndicesRemapped()
        {
            var model = ValidSquare();
            // A fifth vertex sitting exactly on the first, referenced by the second triangle - what
            // an unwelded soup import produces.
            model.Vertices.Add(new Vector2Value(-0.5f, -0.5f));
            model.Indices[3] = 4;

            AssertInvalid<RuleShapeGeometryAttribute>(model);
            AssertFixed(model);
            Assert.AreEqual(4, model.Vertices.Count);
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 0, 2, 3 }, model.Indices);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void WrongType_Throws() => AssertWrongType(new NotGeometryModel());

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Group_IsError()
        {
            var model = Valid();
            (model.Indices[1], model.Indices[2]) = (model.Indices[2], model.Indices[1]);

            AssertGroup<RuleShapeGeometryAttribute>(model, RuleGroup.Error);
        }
    }
}
