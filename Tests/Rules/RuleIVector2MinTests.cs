using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// RuleIVector2Min over all four IVector2 variants. Backs the "no negative extent" fields
    /// (EffectObjectCore.LifetimeBounds, EffectShapeRectangle.Size).
    /// </summary>
    public class RuleIVector2MinTests : BaseRuleTests
    {
        /// <summary> The model the rule under test sits on. </summary>
        [RuleContainer]
        private class Model
        {
            [RuleIVector2Min(0f)]
            public IVector2 Value { get; set; } = new Vector2Value(1f, 1f);
        }

        /// <summary> A bound given per axis rather than one number for all of them. </summary>
        [RuleContainer]
        private class PerAxisModel
        {
            [RuleIVector2Min(0f, -10f)]
            public IVector2 Value { get; set; } = new Vector2Value(1f, 1f);
        }

        /// <summary> A property of a type the rule does not apply to, so it must decline rather than refuse. </summary>
        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleIVector2Min(0f)]
            public float Value { get; set; }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValueValid()
        {
            AssertValid(new Model { Value = new Vector2Value(0f, 5f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValueBelowMinPerAxis()
        {
            AssertInvalid<RuleIVector2MinAttribute>(new Model { Value = new Vector2Value(-0.1f, 5f) });
            AssertInvalid<RuleIVector2MinAttribute>(new Model { Value = new Vector2Value(5f, -0.1f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixValueRaisesEachAxis()
        {
            var model = new Model { Value = new Vector2Value(-5f, -3f) };
            AssertFixed(model);

            var value = (Vector2Value)model.Value;
            Assert.AreEqual(0f, value.X);
            Assert.AreEqual(0f, value.Y);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestPerAxisBounds()
        {
            AssertValid(new PerAxisModel { Value = new Vector2Value(0f, -10f) });
            AssertInvalid<RuleIVector2MinAttribute>(
                new PerAxisModel { Value = new Vector2Value(0f, -11f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestRect()
        {
            AssertValid(new Model { Value = new Vector2Rect(0f, 0f, 5f, 5f) });
            AssertInvalid<RuleIVector2MinAttribute>(
                new Model { Value = new Vector2Rect(-1f, 0f, 5f, 5f) });
        }

        // Only the lower corner is checked here, so a rect whose Max drops below the bound slips
        // past THIS rule - and is caught by Vector2Rect's own per-axis RulePropertyOrder, since such
        // a rect is inverted on both axes. Same division of labour as the IFloat pair.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestInvertedRectCaughtByModelRule()
        {
            AssertInvalid<RulePropertyOrderAttribute>(
                new Model { Value = new Vector2Rect(0f, 0f, -50f, -50f) }, 2);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestRectStep()
        {
            AssertValid(new Model { Value = new Vector2RectStep(0f, 0f, 5f, 5f, 0.5f) });
            AssertInvalid<RuleIVector2MinAttribute>(
                new Model { Value = new Vector2RectStep(0f, -1f, 5f, 5f, 0.5f) });
        }

        // The circle case measures the disc's edge, not its centre.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestCircleUsesDiscEdge()
        {
            AssertValid(new Model { Value = new Vector2Circle(2f, 2f, 2f) });
            AssertInvalid<RuleIVector2MinAttribute>(new Model { Value = new Vector2Circle(1f, 2f, 2f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixCircleShiftsCentre()
        {
            var model = new Model { Value = new Vector2Circle(0f, 0f, 2f) };
            AssertFixed(model);

            var value = (Vector2Circle)model.Value;
            Assert.AreEqual(2f, value.X);
            Assert.AreEqual(2f, value.Y);
            Assert.AreEqual(2f, value.Radius);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNull()
        {
            AssertInvalid<RuleIVector2MinAttribute>(new Model { Value = null });
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
