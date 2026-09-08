using BH.SDK.Models.Enums;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// RuleEnumValid: the rule that turns "ease: 200" from an invisible time bomb into a reported,
    /// fixable issue.
    /// </summary>
    public class RuleEnumValidTests : BaseRuleTests
    {
        /// <summary> An enum with gaps between its values. </summary>
        private enum Sparse : byte
        {
            First = 3,
            Second = 7,
        }

        /// <summary> An enum whose values run consecutively. </summary>
        private enum Dense : byte
        {
            Zero = 0,
            One = 1,
        }

        /// <summary> A real enum from the format, so the rule is exercised against shipped data too. </summary>
        [RuleContainer]
        private class EaseModel
        {
            [RuleEnumValid]
            public EaseType Value { get; set; } = EaseType.Linear;
        }

        /// <summary> The rule left at its own default, with nothing spelled out at the call site. </summary>
        [RuleContainer]
        private class DefaultModel
        {
            [RuleEnumValid(EaseType.InOutSine)]
            public EaseType Value { get; set; } = EaseType.InOutSine;
        }

        /// <summary> A property of the sparse enum, where a value in a gap must be refused. </summary>
        [RuleContainer]
        private class SparseModel
        {
            [RuleEnumValid]
            public Sparse Value { get; set; } = Sparse.First;
        }

        /// <summary> A property of the dense enum. </summary>
        [RuleContainer]
        private class DenseModel
        {
            [RuleEnumValid]
            public Dense Value { get; set; } = Dense.One;
        }

        /// <summary> A property of a type the rule does not apply to, so it must decline rather than refuse. </summary>
        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleEnumValid]
            public int Value { get; set; }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestDeclaredValueIsValid()
        {
            AssertValid(new EaseModel { Value = EaseType.Linear });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestUndeclaredValueIsInvalid()
        {
            AssertInvalid<RuleEnumValidAttribute>(new EaseModel { Value = (EaseType)200 });
        }

        // The value an out-of-range enum most plausibly comes from: a foreign or hand-edited file,
        // where the number is simply not one this build knows.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixFallsBackToZeroWhenDeclared()
        {
            var model = new DenseModel { Value = (Dense)99 };
            AssertFixedTo(model, () => model.Value, Dense.Zero);
        }

        // An enum with no zero member cannot fall back to it, so the first declared value stands in.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixFallsBackToFirstDeclaredWhenNoZero()
        {
            var model = new SparseModel { Value = (Sparse)99 };
            AssertFixedTo(model, () => model.Value, Sparse.First);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixPrefersDeclaredDefault()
        {
            var model = new DefaultModel { Value = (EaseType)200 };
            AssertFixedTo(model, () => model.Value, EaseType.InOutSine);
        }

        // Zero is a legal value of most of the format's enums (Linear, Value, None...), so it must
        // not be treated as "unset and therefore broken".
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestZeroIsValidWhenDeclared()
        {
            AssertValid(new DenseModel { Value = Dense.Zero });
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
