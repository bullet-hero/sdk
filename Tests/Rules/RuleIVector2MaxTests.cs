using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    // Unused by any live model today, same as RuleIFloatMax - covered so the first property that
    // needs it does not discover a broken switch arm.

    /// <summary>
    /// RuleIVector2Max over all four IVector2 variants.
    /// </summary>
    public class RuleIVector2MaxTests : BaseRuleTests
    {
        /// <summary> The model the rule under test sits on. </summary>
        [RuleContainer]
        private class Model
        {
            [RuleIVector2Max(10f)]
            public IVector2 Value { get; set; } = new Vector2Value(0f, 0f);
        }

        /// <summary> A bound given per axis rather than one number for all of them. </summary>
        [RuleContainer]
        private class PerAxisModel
        {
            [RuleIVector2Max(10f, 1f)]
            public IVector2 Value { get; set; } = new Vector2Value(0f, 0f);
        }

        /// <summary> A property of a type the rule does not apply to, so it must decline rather than refuse. </summary>
        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleIVector2Max(10f)]
            public float Value { get; set; }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValueValid()
        {
            AssertValid(new Model { Value = new Vector2Value(10f, -100f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValueAboveMaxPerAxis()
        {
            AssertInvalid<RuleIVector2MaxAttribute>(new Model { Value = new Vector2Value(11f, 0f) });
            AssertInvalid<RuleIVector2MaxAttribute>(new Model { Value = new Vector2Value(0f, 11f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixValueLowersEachAxis()
        {
            var model = new Model { Value = new Vector2Value(50f, 30f) };
            AssertFixed(model);

            var value = (Vector2Value)model.Value;
            Assert.AreEqual(10f, value.X);
            Assert.AreEqual(10f, value.Y);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestPerAxisBounds()
        {
            AssertValid(new PerAxisModel { Value = new Vector2Value(10f, 1f) });
            AssertInvalid<RuleIVector2MaxAttribute>(
                new PerAxisModel { Value = new Vector2Value(10f, 2f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestRect()
        {
            AssertValid(new Model { Value = new Vector2Rect(-50f, -50f, 10f, 10f) });
            AssertInvalid<RuleIVector2MaxAttribute>(
                new Model { Value = new Vector2Rect(-50f, -50f, 11f, 10f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestRectStep()
        {
            AssertValid(new Model { Value = new Vector2RectStep(-50f, -50f, 10f, 10f, 1f) });
            AssertInvalid<RuleIVector2MaxAttribute>(
                new Model { Value = new Vector2RectStep(-50f, -50f, 10f, 11f, 1f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestCircleUsesDiscEdge()
        {
            AssertValid(new Model { Value = new Vector2Circle(8f, 8f, 2f) });
            AssertInvalid<RuleIVector2MaxAttribute>(new Model { Value = new Vector2Circle(9f, 8f, 2f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixCircleShiftsCentre()
        {
            var model = new Model { Value = new Vector2Circle(20f, 20f, 2f) };
            AssertFixed(model);

            var value = (Vector2Circle)model.Value;
            Assert.AreEqual(8f, value.X);
            Assert.AreEqual(8f, value.Y);
            Assert.AreEqual(2f, value.Radius);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNull()
        {
            AssertInvalid<RuleIVector2MaxAttribute>(new Model { Value = null });
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
