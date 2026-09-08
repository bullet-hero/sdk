using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    // Same rationale as RuleIVector3RangeTests: the shared behaviour lives in RuleIVector2*Tests,
    // and what is proven here is that the extra W axis is really part of every check. None of the
    // three IVector4 rules is used by a live model yet.

    /// <summary>
    /// RuleIVector4InRange / RuleIVector4Min / RuleIVector4Max over all four IVector4 variants.
    /// </summary>
    public class RuleIVector4RangeTests : BaseRuleTests
    {
        /// <summary> A 4D vector bounded on every axis. </summary>
        [RuleContainer]
        private class InRangeModel
        {
            [RuleIVector4InRange(-1f, 1f)]
            public IVector4 Value { get; set; } = new Vector4Value(0f, 0f, 0f, 0f);
        }

        /// <summary> A 4D vector with a floor. </summary>
        [RuleContainer]
        private class MinModel
        {
            [RuleIVector4Min(0f)]
            public IVector4 Value { get; set; } = new Vector4Value(1f, 1f, 1f, 1f);
        }

        /// <summary> A 4D vector with a ceiling. </summary>
        [RuleContainer]
        private class MaxModel
        {
            [RuleIVector4Max(10f)]
            public IVector4 Value { get; set; } = new Vector4Value(0f, 0f, 0f, 0f);
        }

        /// <summary> A property of a type the rule does not apply to, so it must decline rather than refuse. </summary>
        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleIVector4InRange(-1f, 1f)]
            public float Value { get; set; }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestInRangeValue()
        {
            AssertValid(new InRangeModel { Value = new Vector4Value(-1f, 0f, 0f, 1f) });
            AssertInvalid<RuleIVector4InRangeAttribute>(
                new InRangeModel { Value = new Vector4Value(0f, 0f, 0f, 2f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestInRangeFixClampsW()
        {
            var model = new InRangeModel { Value = new Vector4Value(0f, 0f, 0f, 5f) };
            AssertFixed(model);

            Assert.AreEqual(1f, ((Vector4Value)model.Value).W);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestInRangeRect()
        {
            AssertValid(new InRangeModel
            {
                Value = new Vector4Rect(-1f, -1f, -1f, -1f, 1f, 1f, 1f, 1f),
            });
            AssertInvalid<RuleIVector4InRangeAttribute>(new InRangeModel
            {
                Value = new Vector4Rect(-1f, -1f, -1f, -2f, 1f, 1f, 1f, 1f),
            });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestInRangeRectStep()
        {
            AssertValid(new InRangeModel
            {
                Value = new Vector4RectStep(-1f, -1f, -1f, -1f, 1f, 1f, 1f, 1f, 0.5f),
            });
            AssertInvalid<RuleIVector4InRangeAttribute>(new InRangeModel
            {
                Value = new Vector4RectStep(-1f, -1f, -1f, -1f, 1f, 1f, 1f, 2f, 0.5f),
            });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestInRangeCircle()
        {
            AssertValid(new InRangeModel { Value = new Vector4Circle(0f, 0f, 0f, 0f, 1f) });
            AssertInvalid<RuleIVector4InRangeAttribute>(
                new InRangeModel { Value = new Vector4Circle(0f, 0f, 0f, 0.5f, 1f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMin()
        {
            AssertValid(new MinModel { Value = new Vector4Value(0f, 0f, 0f, 0f) });
            AssertInvalid<RuleIVector4MinAttribute>(
                new MinModel { Value = new Vector4Value(0f, 0f, 0f, -1f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMinFixRaisesW()
        {
            var model = new MinModel { Value = new Vector4Value(0f, 0f, 0f, -5f) };
            AssertFixed(model);

            Assert.AreEqual(0f, ((Vector4Value)model.Value).W);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMinRectAndCircle()
        {
            AssertValid(new MinModel
            {
                Value = new Vector4Rect(0f, 0f, 0f, 0f, 5f, 5f, 5f, 5f),
            });
            AssertInvalid<RuleIVector4MinAttribute>(new MinModel
            {
                Value = new Vector4Rect(0f, 0f, 0f, -1f, 5f, 5f, 5f, 5f),
            });

            AssertValid(new MinModel { Value = new Vector4Circle(2f, 2f, 2f, 2f, 2f) });
            AssertInvalid<RuleIVector4MinAttribute>(
                new MinModel { Value = new Vector4Circle(2f, 2f, 2f, 1f, 2f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMax()
        {
            AssertValid(new MaxModel { Value = new Vector4Value(10f, 10f, 10f, 10f) });
            AssertInvalid<RuleIVector4MaxAttribute>(
                new MaxModel { Value = new Vector4Value(0f, 0f, 0f, 11f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMaxFixLowersW()
        {
            var model = new MaxModel { Value = new Vector4Value(0f, 0f, 0f, 50f) };
            AssertFixed(model);

            Assert.AreEqual(10f, ((Vector4Value)model.Value).W);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMaxRectStepAndCircle()
        {
            AssertValid(new MaxModel
            {
                Value = new Vector4RectStep(-5f, -5f, -5f, -5f, 10f, 10f, 10f, 10f, 1f),
            });
            AssertInvalid<RuleIVector4MaxAttribute>(new MaxModel
            {
                Value = new Vector4RectStep(-5f, -5f, -5f, -5f, 10f, 10f, 10f, 11f, 1f),
            });

            AssertValid(new MaxModel { Value = new Vector4Circle(8f, 8f, 8f, 8f, 2f) });
            AssertInvalid<RuleIVector4MaxAttribute>(
                new MaxModel { Value = new Vector4Circle(8f, 8f, 8f, 9f, 2f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNull()
        {
            AssertInvalid<RuleIVector4InRangeAttribute>(new InRangeModel { Value = null });
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
