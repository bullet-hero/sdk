using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// RuleMaxValue: value must be &lt;= Max. Fix clamps to DefaultValue when set, to Max otherwise.
    /// </summary>
    public class RuleMaxTests : BaseRuleTests
    {
        [RuleContainer]
        private class IntModel
        {
            [RuleMaxValue(10)]
            public int Value { get; set; } = 10;
        }

        [RuleContainer]
        private class IntDefaultModel
        {
            [RuleMaxValue(10, -3)]
            public int Value { get; set; } = -3;
        }

        [RuleContainer]
        private class FloatModel
        {
            [RuleMaxValue(1.5f)]
            public float Value { get; set; } = 1.5f;
        }

        [RuleContainer]
        private class ByteModel
        {
            [RuleMaxValue((byte)5)]
            public byte Value { get; set; } = 5;
        }

        [RuleContainer]
        private class ULongModel
        {
            [RuleMaxValue(5UL)]
            public ulong Value { get; set; } = 5UL;
        }

        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleMaxValue(1)]
            public string Value { get; set; } = "text";
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestValid()
        {
            AssertValid(new IntModel { Value = -100 });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestBoundary()
        {
            AssertValid(new IntModel { Value = 10 });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestJustOver()
        {
            AssertInvalid<RuleMaxValueAttribute>(new IntModel { Value = 11 });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestFixClampsToMax()
        {
            var model = new IntModel { Value = 500 };
            AssertFixedTo(model, () => model.Value, 10);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestFixUsesDefaultValue()
        {
            var model = new IntDefaultModel { Value = 500 };
            AssertFixedTo(model, () => model.Value, -3);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestByte()
        {
            AssertValid(new ByteModel { Value = 5 });
            AssertInvalid<RuleMaxValueAttribute>(new ByteModel { Value = 6 });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestULong()
        {
            AssertValid(new ULongModel { Value = 5UL });
            AssertInvalid<RuleMaxValueAttribute>(new ULongModel { Value = 6UL });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestFloat()
        {
            AssertValid(new FloatModel { Value = 1.5f });
            AssertInvalid<RuleMaxValueAttribute>(new FloatModel { Value = 1.51f });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestPositiveInfinityIsInvalid()
        {
            AssertInvalid<RuleMaxValueAttribute>(new FloatModel { Value = float.PositiveInfinity });
        }

        // NaN sorts below every real number, so an upper bound alone accepts it - this is exactly
        // the hole RuleFiniteNumber has to cover. RuleInRange does NOT have it, because its lower-bound
        // half rejects NaN first. Locked down as a test so the asymmetry stays visible.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestNaNPassesUpperBoundOnly()
        {
            AssertValid(new FloatModel { Value = float.NaN });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestNegativeInfinityIsValid()
        {
            AssertValid(new FloatModel { Value = float.NegativeInfinity });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestWrongType()
        {
            AssertWrongType(new WrongTypeModel());
        }
    }
}
