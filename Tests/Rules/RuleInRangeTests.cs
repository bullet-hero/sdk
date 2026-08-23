using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// RuleInRange: Min &lt;= value &lt;= Max. The workhorse of the format - 192 of the ~470 ruled
    /// properties use it, so its boundary and non-finite behaviour is what most limits actually mean.
    /// </summary>
    public class RuleInRangeTests : BaseRuleTests
    {
        [RuleContainer]
        private class IntModel
        {
            [RuleInRange(-10, 10)]
            public int Value { get; set; }
        }

        [RuleContainer]
        private class IntDefaultModel
        {
            [RuleInRange(-10, 10, 7)]
            public int Value { get; set; } = 7;
        }

        [RuleContainer]
        private class FloatModel
        {
            [RuleInRange(0f, 1f)]
            public float Value { get; set; } = 0.5f;
        }

        [RuleContainer]
        private class SByteModel
        {
            [RuleInRange((sbyte)-5, (sbyte)5)]
            public sbyte Value { get; set; }
        }

        [RuleContainer]
        private class ShortModel
        {
            [RuleInRange((short)-5, (short)5)]
            public short Value { get; set; }
        }

        [RuleContainer]
        private class UShortModel
        {
            [RuleInRange((ushort)1, (ushort)5)]
            public ushort Value { get; set; } = 1;
        }

        [RuleContainer]
        private class UIntModel
        {
            [RuleInRange(1u, 5u)]
            public uint Value { get; set; } = 1u;
        }

        [RuleContainer]
        private class DoubleModel
        {
            [RuleInRange(0d, 1d)]
            public double Value { get; set; } = 0.5d;
        }

        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleInRange(0, 1)]
            public string Value { get; set; } = "text";
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValid()
        {
            AssertValid(new IntModel { Value = 0 });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestLowerBoundary()
        {
            AssertValid(new IntModel { Value = -10 });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestUpperBoundary()
        {
            AssertValid(new IntModel { Value = 10 });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestJustUnder()
        {
            AssertInvalid<RuleInRangeAttribute>(new IntModel { Value = -11 });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestJustOver()
        {
            AssertInvalid<RuleInRangeAttribute>(new IntModel { Value = 11 });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixClampsToMin()
        {
            var model = new IntModel { Value = -500 };
            AssertFixedTo(model, () => model.Value, -10);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixClampsToMax()
        {
            var model = new IntModel { Value = 500 };
            AssertFixedTo(model, () => model.Value, 10);
        }

        // DefaultValue wins over the nearest bound in BOTH directions - an over-max value does not
        // clamp down to Max, it jumps to DefaultValue. Easy to assume otherwise from the name.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixUsesDefaultValueBelowMin()
        {
            var model = new IntDefaultModel { Value = -500 };
            AssertFixedTo(model, () => model.Value, 7);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixUsesDefaultValueAboveMax()
        {
            var model = new IntDefaultModel { Value = 500 };
            AssertFixedTo(model, () => model.Value, 7);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestSByte()
        {
            AssertValid(new SByteModel { Value = -5 });
            AssertInvalid<RuleInRangeAttribute>(new SByteModel { Value = -6 });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestShort()
        {
            AssertValid(new ShortModel { Value = 5 });
            AssertInvalid<RuleInRangeAttribute>(new ShortModel { Value = 6 });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestUShort()
        {
            AssertValid(new UShortModel { Value = 5 });
            AssertInvalid<RuleInRangeAttribute>(new UShortModel { Value = 6 });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestUInt()
        {
            AssertValid(new UIntModel { Value = 5u });
            AssertInvalid<RuleInRangeAttribute>(new UIntModel { Value = 6u });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestDouble()
        {
            AssertValid(new DoubleModel { Value = 1d });
            AssertInvalid<RuleInRangeAttribute>(new DoubleModel { Value = 1.0001d });
        }

        // A two-sided range is the only numeric rule that rejects every non-finite value, because
        // NaN fails the lower bound and the infinities fail one side each. This is what makes
        // RuleInRange - and not RuleMinValue/RuleMaxValue - the safe default for authored floats.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNaNIsInvalid()
        {
            AssertInvalid<RuleInRangeAttribute>(new FloatModel { Value = float.NaN });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestInfinitiesAreInvalid()
        {
            AssertInvalid<RuleInRangeAttribute>(new FloatModel { Value = float.PositiveInfinity });
            AssertInvalid<RuleInRangeAttribute>(new FloatModel { Value = float.NegativeInfinity });
        }

        // NaN takes the below-Min branch, so it is repaired to Min - not left as NaN, and not
        // pushed to Max. Worth pinning: a NaN surviving a fix would poison every downstream job.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixNaNToMin()
        {
            var model = new FloatModel { Value = float.NaN };
            AssertFixedTo(model, () => model.Value, 0f);
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
