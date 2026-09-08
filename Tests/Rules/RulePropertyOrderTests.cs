using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    // The first object-level rule, so these tests double as coverage of that machinery: that a
    // class-level attribute is discovered at all, that it is reported against the object rather than
    // a property, and that RuleFixer can repair it without a PropertyInfo to aim at.

    /// <summary>
    /// RulePropertyOrder: two properties of one object form an ordered pair. Covers every "these two
    /// fields are a range" invariant - Min/Max, aspect bounds, camera sizes.
    /// </summary>
    public class RulePropertyOrderTests : BaseRuleTests
    {
        // nameof has to be qualified: a class-level attribute sits outside the class body, so its
        // own members are not in scope there. Same applies to every real model in Models/.

        /// <summary> Two floats that must stay in order. </summary>
        [RuleContainer]
        [RulePropertyOrder(nameof(FloatPairModel.Min), nameof(FloatPairModel.Max))]
        private class FloatPairModel
        {
            public float Min { get; set; }
            public float Max { get; set; } = 1f;
        }

        /// <summary> Two frame numbers that must stay in order. </summary>
        [RuleContainer]
        [RulePropertyOrder(nameof(FramePairModel.StartFrame), nameof(FramePairModel.EndFrame))]
        private class FramePairModel
        {
            public int StartFrame { get; set; }
            public int EndFrame { get; set; } = 10;
        }

        /// <summary> Two independent ordered pairs on one model. </summary>
        [RuleContainer]
        [RulePropertyOrder(nameof(TwoPairModel.MinR), nameof(TwoPairModel.MaxR))]
        [RulePropertyOrder(nameof(TwoPairModel.MinG), nameof(TwoPairModel.MaxG))]
        private class TwoPairModel
        {
            public float MinR { get; set; }
            public float MaxR { get; set; } = 1f;
            public float MinG { get; set; }
            public float MaxG { get; set; } = 1f;
        }

        /// <summary> Names a partner property that does not exist, so the DECLARATION is what must be refused. </summary>
        [RuleContainer]
        [RulePropertyOrder(nameof(MissingPropertyModel.Min), "NoSuchProperty")]
        private class MissingPropertyModel
        {
            public float Min { get; set; }
        }

        /// <summary> Names a partner of another type, which cannot be compared. </summary>
        [RuleContainer]
        [RulePropertyOrder(nameof(MismatchedTypesModel.Low), nameof(MismatchedTypesModel.High))]
        private class MismatchedTypesModel
        {
            public float Low { get; set; }
            public string High { get; set; } = string.Empty;
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestOrdered()
        {
            AssertValid(new FloatPairModel { Min = 0f, Max = 1f });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestEqualIsOrdered()
        {
            AssertValid(new FloatPairModel { Min = 5f, Max = 5f });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestInverted()
        {
            AssertInvalid<RulePropertyOrderAttribute>(new FloatPairModel { Min = 5f, Max = 1f });
        }

        // Swapping keeps both authored numbers; clamping one onto the other would collapse the range
        // and silently discard whichever value the author actually meant.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixSwaps()
        {
            var model = new FloatPairModel { Min = 5f, Max = 1f };
            AssertFixed(model);

            Assert.AreEqual(1f, model.Min);
            Assert.AreEqual(5f, model.Max);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFramePair()
        {
            AssertValid(new FramePairModel { StartFrame = 0, EndFrame = 10 });

            var model = new FramePairModel { StartFrame = 90, EndFrame = 10 };
            AssertFixed(model);

            Assert.AreEqual(10, model.StartFrame);
            Assert.AreEqual(90, model.EndFrame);
        }

        // A type can hold several independent pairs - Color4MinMax has four - so the attribute has
        // to be repeatable and each instance reported on its own.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMultiplePairsReportedIndependently()
        {
            AssertValid(new TwoPairModel());

            AssertInvalid<RulePropertyOrderAttribute>(new TwoPairModel { MinR = 5f, MaxR = 1f });
            AssertInvalid<RulePropertyOrderAttribute>(
                new TwoPairModel { MinR = 5f, MaxR = 1f, MinG = 5f, MaxG = 1f }, 2);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixRepairsEveryPair()
        {
            var model = new TwoPairModel { MinR = 5f, MaxR = 1f, MinG = 9f, MaxG = 2f };
            AssertFixed(model);

            Assert.AreEqual(1f, model.MinR);
            Assert.AreEqual(5f, model.MaxR);
            Assert.AreEqual(2f, model.MinG);
            Assert.AreEqual(9f, model.MaxG);
        }

        // Same contract as a property rule pointed at the wrong type: naming a property that does not
        // exist, or two of different types, is a declaration error and fails loudly rather than
        // quietly passing everything.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMissingPropertyIsRejected()
        {
            AssertWrongType(new MissingPropertyModel());
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMismatchedTypesAreRejected()
        {
            AssertWrongType(new MismatchedTypesModel());
        }
    }
}
