using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// RuleIVector2InRange over all four IVector2 variants. The RandomCircle case is the only rule
    /// in the SDK that reasons about a shape rather than a component, so it gets the most cases.
    /// </summary>
    public class RuleIVector2InRangeTests : BaseRuleTests
    {
        /// <summary> The model the rule under test sits on. </summary>
        [RuleContainer]
        private class Model
        {
            [RuleIVector2InRange(-1f, 1f)]
            public IVector2 Value { get; set; } = new Vector2Value(0f, 0f);
        }

        /// <summary> Bounds that differ between the axes. </summary>
        [RuleContainer]
        private class AsymmetricModel
        {
            [RuleIVector2InRange(0f, 10f, -5f, 5f)]
            public IVector2 Value { get; set; } = new Vector2Value(0f, 0f);
        }

        /// <summary> A property of a type the rule does not apply to, so it must decline rather than refuse. </summary>
        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleIVector2InRange(-1f, 1f)]
            public float Value { get; set; }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValueValid()
        {
            AssertValid(new Model { Value = new Vector2Value(0.5f, -0.5f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValueBoundaries()
        {
            AssertValid(new Model { Value = new Vector2Value(-1f, 1f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValueOutOfRangePerAxis()
        {
            AssertInvalid<RuleIVector2InRangeAttribute>(new Model { Value = new Vector2Value(2f, 0f) });
            AssertInvalid<RuleIVector2InRangeAttribute>(new Model { Value = new Vector2Value(0f, -2f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixValueClampsPerAxis()
        {
            var model = new Model { Value = new Vector2Value(5f, -5f) };
            AssertFixed(model);

            var value = (Vector2Value)model.Value;
            Assert.AreEqual(1f, value.X);
            Assert.AreEqual(-1f, value.Y);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestAsymmetricBounds()
        {
            AssertValid(new AsymmetricModel { Value = new Vector2Value(10f, -5f) });
            AssertInvalid<RuleIVector2InRangeAttribute>(
                new AsymmetricModel { Value = new Vector2Value(-1f, 0f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestRect()
        {
            AssertValid(new Model { Value = new Vector2Rect(-1f, -1f, 1f, 1f) });
            AssertInvalid<RuleIVector2InRangeAttribute>(
                new Model { Value = new Vector2Rect(-2f, -1f, 1f, 1f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixRectClampsEachEdge()
        {
            var model = new Model { Value = new Vector2Rect(-5f, -5f, 5f, 5f) };
            AssertFixed(model);

            var value = (Vector2Rect)model.Value;
            Assert.AreEqual(-1f, value.MinX);
            Assert.AreEqual(-1f, value.MinY);
            Assert.AreEqual(1f, value.MaxX);
            Assert.AreEqual(1f, value.MaxY);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestRectStep()
        {
            AssertValid(new Model { Value = new Vector2RectStep(-1f, -1f, 1f, 1f, 0.5f) });
            AssertInvalid<RuleIVector2InRangeAttribute>(
                new Model { Value = new Vector2RectStep(-1f, -1f, 2f, 1f, 0.5f) });
        }

        // A circle is legal when it fits entirely inside the box: radius no larger than half of the
        // shorter side, and the whole disc within bounds - not just its centre.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestCircleInscribed()
        {
            AssertValid(new Model { Value = new Vector2Circle(0f, 0f, 1f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestCircleTooLarge()
        {
            AssertInvalid<RuleIVector2InRangeAttribute>(
                new Model { Value = new Vector2Circle(0f, 0f, 1.5f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestCircleCentreInsideButDiscOutside()
        {
            AssertInvalid<RuleIVector2InRangeAttribute>(
                new Model { Value = new Vector2Circle(0.9f, 0f, 0.5f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixCircleShrinksThenRecentres()
        {
            var model = new Model { Value = new Vector2Circle(5f, 0f, 3f) };
            AssertFixed(model);

            var value = (Vector2Circle)model.Value;
            Assert.AreEqual(1f, value.Radius);
            Assert.AreEqual(0f, value.X);
        }

        // A negative radius slips past THIS rule - every check here compares the radius against an
        // upper bound or expands the centre by it, and a negative value quietly shrinks both sides
        // inward. What catches it is Vector2Circle's own lower bound on Radius, reached when the
        // analyzer walks into the value. Worth pinning: the guarantee comes from the model, not from
        // the range rule pointed at it.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNegativeRadiusCaughtByModelRule()
        {
            AssertInvalid<RuleInRangeAttribute>(new Model { Value = new Vector2Circle(0f, 0f, -1f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNull()
        {
            AssertInvalid<RuleIVector2InRangeAttribute>(new Model { Value = null });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestWrongType()
        {
            AssertWrongType(new WrongTypeModel());
        }
    }
}
