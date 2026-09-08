using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    // No live model uses RuleIFloatMax today - it is covered here anyway, because an untested rule
    // that is one attribute away from being needed is exactly the one that breaks when first used.

    /// <summary>
    /// RuleIFloatMax over all three IFloat variants. Mirror image of RuleIFloatMin, including its
    /// one-sided blind spot on random ranges.
    /// </summary>
    public class RuleIFloatMaxTests : BaseRuleTests
    {
        /// <summary> The model the rule under test sits on. </summary>
        [RuleContainer]
        private class Model
        {
            [RuleIFloatMax(1f)]
            public IFloat Value { get; set; } = new FloatValue(0f);
        }

        /// <summary> A property of a type the rule does not apply to, so it must decline rather than refuse. </summary>
        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleIFloatMax(1f)]
            public float Value { get; set; }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValueValid()
        {
            AssertValid(new Model { Value = new FloatValue(-100f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValueBoundary()
        {
            AssertValid(new Model { Value = new FloatValue(1f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValueAboveMax()
        {
            AssertInvalid<RuleIFloatMaxAttribute>(new Model { Value = new FloatValue(1.1f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixValueLowersToMax()
        {
            var model = new Model { Value = new FloatValue(50f) };
            AssertFixedTo(model, () => ((FloatValue)model.Value).Value, 1f);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMinMaxChecksMax()
        {
            AssertValid(new Model { Value = new FloatMinMax(-10f, 1f) });
            AssertInvalid<RuleIFloatMaxAttribute>(new Model { Value = new FloatMinMax(-10f, 2f) });
        }

        // Symmetric to RuleIFloatMin: the Min end of a random range is never compared against the
        // bound here, and a range starting above Max is necessarily inverted - so FloatMinMax's own
        // RulePropertyOrder is what reports it.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMinEndAboveBoundCaughtByModelRule()
        {
            AssertInvalid<RulePropertyOrderAttribute>(new Model { Value = new FloatMinMax(100f, 0f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixMinMaxLowersMaxOnly()
        {
            var model = new Model { Value = new FloatMinMax(-10f, 50f) };
            AssertFixed(model);

            var value = (FloatMinMax)model.Value;
            Assert.AreEqual(-10f, value.Min);
            Assert.AreEqual(1f, value.Max);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMinMaxStep()
        {
            AssertValid(new Model { Value = new FloatMinMaxStep(-10f, 1f, 1f) });
            AssertInvalid<RuleIFloatMaxAttribute>(
                new Model { Value = new FloatMinMaxStep(-10f, 2f, 1f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNull()
        {
            AssertInvalid<RuleIFloatMaxAttribute>(new Model { Value = null });
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
