using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// RuleIFloatMin over all three IFloat variants. Used for the "cannot be negative" fields of the
    /// effect shapes (radius, height, thickness).
    /// </summary>
    public class RuleIFloatMinTests : BaseRuleTests
    {
        /// <summary> The model the rule under test sits on. </summary>
        [RuleContainer]
        private class Model
        {
            [RuleIFloatMin(0f)]
            public IFloat Value { get; set; } = new FloatValue(1f);
        }

        /// <summary> A property of a type the rule does not apply to, so it must decline rather than refuse. </summary>
        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleIFloatMin(0f)]
            public float Value { get; set; }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValueValid()
        {
            AssertValid(new Model { Value = new FloatValue(1f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValueBoundary()
        {
            AssertValid(new Model { Value = new FloatValue(0f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValueBelowMin()
        {
            AssertInvalid<RuleIFloatMinAttribute>(new Model { Value = new FloatValue(-0.1f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixValueRaisesToMin()
        {
            var model = new Model { Value = new FloatValue(-5f) };
            AssertFixedTo(model, () => ((FloatValue)model.Value).Value, 0f);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMinMaxChecksMin()
        {
            AssertValid(new Model { Value = new FloatMinMax(0f, 10f) });
            AssertInvalid<RuleIFloatMinAttribute>(new Model { Value = new FloatMinMax(-1f, 10f) });
        }

        // Only the Min end of a random range is checked here, so a range whose Max sits below the
        // bound - one that can only ever roll an illegal value - slips past THIS rule. It does not
        // slip past validation: such a range is necessarily inverted, and FloatMinMax's own
        // RulePropertyOrder reports it. Pinned so the division of labour stays visible.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMaxEndBelowBoundCaughtByModelRule()
        {
            AssertInvalid<RulePropertyOrderAttribute>(new Model { Value = new FloatMinMax(5f, -100f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixMinMaxRaisesMinOnly()
        {
            var model = new Model { Value = new FloatMinMax(-5f, 10f) };
            AssertFixed(model);

            var value = (FloatMinMax)model.Value;
            Assert.AreEqual(0f, value.Min);
            Assert.AreEqual(10f, value.Max);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMinMaxStep()
        {
            AssertValid(new Model { Value = new FloatMinMaxStep(0f, 10f, 1f) });
            AssertInvalid<RuleIFloatMinAttribute>(
                new Model { Value = new FloatMinMaxStep(-1f, 10f, 1f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNull()
        {
            AssertInvalid<RuleIFloatMinAttribute>(new Model { Value = null });
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
