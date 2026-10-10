using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    // The I* rules switch over the concrete variant, so every variant of the interface gets a case. Bounds
    // are narrow (0..1) so only the rule under test fires.

    /// <summary>
    /// RuleIFloatInRange over all three IFloat variants: FloatValue, FloatMinMax, FloatMinMaxStep.
    /// </summary>
    public class RuleIFloatInRangeTests : BaseRuleTests
    {
        /// <summary> The model the rule under test sits on. </summary>
        [RuleContainer]
        private class Model
        {
            [RuleIFloatInRange(0f, 1f)]
            public IFloat Value { get; set; } = new FloatValue(0.5f);
        }

        /// <summary> A property of a type the rule does not apply to, so it must decline rather than refuse. </summary>
        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleIFloatInRange(0f, 1f)]
            public float Value { get; set; }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValueValid()
        {
            AssertValid(new Model { Value = new FloatValue(0.5f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValueBoundaries()
        {
            AssertValid(new Model { Value = new FloatValue(0f) });
            AssertValid(new Model { Value = new FloatValue(1f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValueBelowMin()
        {
            AssertInvalid<RuleIFloatInRangeAttribute>(new Model { Value = new FloatValue(-0.1f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValueAboveMax()
        {
            AssertInvalid<RuleIFloatInRangeAttribute>(new Model { Value = new FloatValue(1.1f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixValueClamps()
        {
            var model = new Model { Value = new FloatValue(5f) };
            AssertFixedTo(model, () => ((FloatValue)model.Value).Value, 1f);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMinMaxValid()
        {
            AssertValid(new Model { Value = new FloatMinMax(0.2f, 0.8f) });
        }

        // Only the outer edges: ordering is FloatMinMax's own RulePropertyOrder.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestInvertedPairCaughtByModelRule()
        {
            AssertInvalid<RulePropertyOrderAttribute>(new Model { Value = new FloatMinMax(0.8f, 0.2f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMinMaxOutOfRange()
        {
            AssertInvalid<RuleIFloatInRangeAttribute>(new Model { Value = new FloatMinMax(-1f, 0.5f) });
            AssertInvalid<RuleIFloatInRangeAttribute>(new Model { Value = new FloatMinMax(0.5f, 2f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixMinMaxClampsBothEnds()
        {
            var model = new Model { Value = new FloatMinMax(-3f, 4f) };
            AssertFixed(model);

            var value = (FloatMinMax)model.Value;
            Assert.AreEqual(0f, value.Min);
            Assert.AreEqual(1f, value.Max);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMinMaxStepValid()
        {
            AssertValid(new Model { Value = new FloatMinMaxStep(0.2f, 0.8f, 0.1f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMinMaxStepOutOfRange()
        {
            AssertInvalid<RuleIFloatInRangeAttribute>(
                new Model { Value = new FloatMinMaxStep(-1f, 0.5f, 0.1f) });
        }

        // Step is untouched by the range rule - a step larger than the whole range stays valid.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixMinMaxStepLeavesStepAlone()
        {
            var model = new Model { Value = new FloatMinMaxStep(-3f, 4f, 0.25f) };
            AssertFixed(model);

            var value = (FloatMinMaxStep)model.Value;
            Assert.AreEqual(0f, value.Min);
            Assert.AreEqual(1f, value.Max);
            Assert.AreEqual(0.25f, value.Step);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNull()
        {
            AssertInvalid<RuleIFloatInRangeAttribute>(new Model { Value = null });
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
