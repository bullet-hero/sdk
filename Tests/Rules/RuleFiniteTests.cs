using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// RuleFiniteNumber: rejects NaN and the infinities on the floats no two-sided range covers.
    /// </summary>
    public class RuleFiniteTests : BaseRuleTests
    {
        [RuleContainer]
        private class FloatModel
        {
            [RuleFiniteNumber]
            public float Value { get; set; }
        }

        [RuleContainer]
        private class DoubleModel
        {
            [RuleFiniteNumber]
            public double Value { get; set; }
        }

        [RuleContainer]
        private class DefaultModel
        {
            [RuleFiniteNumber(1.5f)]
            public float Value { get; set; }
        }

        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleFiniteNumber]
            public int Value { get; set; }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestOrdinaryValues()
        {
            AssertValid(new FloatModel { Value = 0f });
            AssertValid(new FloatModel { Value = -12345.678f });
            AssertValid(new FloatModel { Value = float.MaxValue });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNaN()
        {
            AssertInvalid<RuleFiniteNumberAttribute>(new FloatModel { Value = float.NaN });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestInfinities()
        {
            AssertInvalid<RuleFiniteNumberAttribute>(new FloatModel { Value = float.PositiveInfinity });
            AssertInvalid<RuleFiniteNumberAttribute>(new FloatModel { Value = float.NegativeInfinity });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestDouble()
        {
            AssertValid(new DoubleModel { Value = 1d });
            AssertInvalid<RuleFiniteNumberAttribute>(new DoubleModel { Value = double.NaN });
            AssertInvalid<RuleFiniteNumberAttribute>(new DoubleModel { Value = double.NegativeInfinity });
        }

        // Zero rather than a bound: the rule exists precisely where there is no bound to clamp to.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixToZero()
        {
            var model = new FloatModel { Value = float.NaN };
            AssertFixedTo(model, () => model.Value, 0f);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixToDeclaredDefault()
        {
            var model = new DefaultModel { Value = float.PositiveInfinity };
            AssertFixedTo(model, () => model.Value, 1.5f);
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
