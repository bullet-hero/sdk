using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// RuleIVector2Ordered: for the IVector2 properties that are ranges rather than points
    /// (LifetimeBounds, SpeedRange, the shadow/highlight limit pairs). Random variants must be
    /// ordered for every value they can produce, not just on average.
    /// </summary>
    public class RuleIVector2OrderedTests : BaseRuleTests
    {
        /// <summary> The model the rule under test sits on. </summary>
        [RuleContainer]
        private class Model
        {
            [RuleIVector2Ordered]
            public IVector2 Value { get; set; } = new Vector2Value(0f, 1f);
        }

        /// <summary> A property of a type the rule does not apply to, so it must decline rather than refuse. </summary>
        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleIVector2Ordered]
            public float Value { get; set; }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValueOrdered()
        {
            AssertValid(new Model { Value = new Vector2Value(0f, 1f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValueDegenerateRangeIsValid()
        {
            AssertValid(new Model { Value = new Vector2Value(3f, 3f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValueInverted()
        {
            AssertInvalid<RuleIVector2OrderedAttribute>(new Model { Value = new Vector2Value(5f, 1f) });
        }

        // Swap, not clamp: an inverted pair is the same two numbers in the wrong order, and swapping
        // keeps the range the author meant instead of collapsing it to a point.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixValueSwaps()
        {
            var model = new Model { Value = new Vector2Value(5f, 1f) };
            AssertFixed(model);

            var value = (Vector2Value)model.Value;
            Assert.AreEqual(1f, value.X);
            Assert.AreEqual(5f, value.Y);
        }

        // Strict on random ranges: the X span must sit entirely below the Y span, because any
        // overlap means some roll produces an inverted pair.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestRectDisjointSpansAreValid()
        {
            AssertValid(new Model { Value = new Vector2Rect(0f, 10f, 5f, 20f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestRectOverlappingSpansAreInvalid()
        {
            AssertInvalid<RuleIVector2OrderedAttribute>(
                new Model { Value = new Vector2Rect(0f, 3f, 5f, 20f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixRectSwapsAxes()
        {
            var model = new Model { Value = new Vector2Rect(10f, 0f, 20f, 5f) };
            AssertFixed(model);

            var value = (Vector2Rect)model.Value;
            Assert.AreEqual(0f, value.MinX);
            Assert.AreEqual(5f, value.MaxX);
            Assert.AreEqual(10f, value.MinY);
            Assert.AreEqual(20f, value.MaxY);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestRectStep()
        {
            AssertValid(new Model { Value = new Vector2RectStep(0f, 10f, 5f, 20f, 1f) });
            AssertInvalid<RuleIVector2OrderedAttribute>(
                new Model { Value = new Vector2RectStep(0f, 3f, 5f, 20f, 1f) });
        }

        // Both components of a circle are drawn from one disc, so ordering survives only if the
        // whole X extent clears the whole Y extent.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestCircleSeparatedExtents()
        {
            AssertValid(new Model { Value = new Vector2Circle(0f, 10f, 1f) });
            AssertInvalid<RuleIVector2OrderedAttribute>(new Model { Value = new Vector2Circle(0f, 1f, 1f) });
        }

        // Swapping a circle's centre cannot fix an overlap - the radius reaches both ways - so the
        // repair collapses the radius and keeps the authored centre.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixCircleCollapsesRadius()
        {
            var model = new Model { Value = new Vector2Circle(0f, 1f, 1f) };
            AssertFixed(model);

            var value = (Vector2Circle)model.Value;
            Assert.AreEqual(0f, value.Radius);
            Assert.AreEqual(0f, value.X);
            Assert.AreEqual(1f, value.Y);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNull()
        {
            AssertInvalid<RuleIVector2OrderedAttribute>(new Model { Value = null });
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
