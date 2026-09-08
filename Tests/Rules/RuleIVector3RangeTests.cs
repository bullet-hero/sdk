using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    // The three IVector3 rules share one file: they are the same switch as their IVector2 siblings
    // with a Z axis bolted on, so the interesting behaviour (one-sided blind spot on random ranges,
    // disc-edge measurement for circles) is documented once in RuleIVector2*Tests. What must be
    // proven separately here is that the Z axis is actually checked - a copy-paste that forgot it
    // would pass every X/Y test.
    //
    // None of the three is used by a live model yet.

    /// <summary>
    /// RuleIVector3InRange / RuleIVector3Min / RuleIVector3Max over all four IVector3 variants.
    /// </summary>
    public class RuleIVector3RangeTests : BaseRuleTests
    {
        /// <summary> A 3D vector bounded on every axis. </summary>
        [RuleContainer]
        private class InRangeModel
        {
            [RuleIVector3InRange(-1f, 1f)]
            public IVector3 Value { get; set; } = new Vector3Value(0f, 0f, 0f);
        }

        /// <summary> A 3D vector with a floor. </summary>
        [RuleContainer]
        private class MinModel
        {
            [RuleIVector3Min(0f)]
            public IVector3 Value { get; set; } = new Vector3Value(1f, 1f, 1f);
        }

        /// <summary> A 3D vector with a ceiling. </summary>
        [RuleContainer]
        private class MaxModel
        {
            [RuleIVector3Max(10f)]
            public IVector3 Value { get; set; } = new Vector3Value(0f, 0f, 0f);
        }

        /// <summary> A property of a type the rule does not apply to, so it must decline rather than refuse. </summary>
        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleIVector3InRange(-1f, 1f)]
            public float Value { get; set; }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestInRangeValue()
        {
            AssertValid(new InRangeModel { Value = new Vector3Value(-1f, 0f, 1f) });
            AssertInvalid<RuleIVector3InRangeAttribute>(
                new InRangeModel { Value = new Vector3Value(0f, 0f, 2f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestInRangeFixClampsZ()
        {
            var model = new InRangeModel { Value = new Vector3Value(0f, 0f, 5f) };
            AssertFixed(model);

            Assert.AreEqual(1f, ((Vector3Value)model.Value).Z);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestInRangeRect()
        {
            AssertValid(new InRangeModel { Value = new Vector3Rect(-1f, -1f, -1f, 1f, 1f, 1f) });
            AssertInvalid<RuleIVector3InRangeAttribute>(
                new InRangeModel { Value = new Vector3Rect(-1f, -1f, -2f, 1f, 1f, 1f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestInRangeRectStep()
        {
            AssertValid(new InRangeModel { Value = new Vector3RectStep(-1f, -1f, -1f, 1f, 1f, 1f, 0.5f) });
            AssertInvalid<RuleIVector3InRangeAttribute>(
                new InRangeModel { Value = new Vector3RectStep(-1f, -1f, -1f, 1f, 1f, 2f, 0.5f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestInRangeCircle()
        {
            AssertValid(new InRangeModel { Value = new Vector3Circle(0f, 0f, 0f, 1f) });
            AssertInvalid<RuleIVector3InRangeAttribute>(
                new InRangeModel { Value = new Vector3Circle(0f, 0f, 0.5f, 1f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMin()
        {
            AssertValid(new MinModel { Value = new Vector3Value(0f, 0f, 0f) });
            AssertInvalid<RuleIVector3MinAttribute>(
                new MinModel { Value = new Vector3Value(0f, 0f, -1f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMinFixRaisesZ()
        {
            var model = new MinModel { Value = new Vector3Value(0f, 0f, -5f) };
            AssertFixed(model);

            Assert.AreEqual(0f, ((Vector3Value)model.Value).Z);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMinRectAndCircle()
        {
            AssertValid(new MinModel { Value = new Vector3Rect(0f, 0f, 0f, 5f, 5f, 5f) });
            AssertInvalid<RuleIVector3MinAttribute>(
                new MinModel { Value = new Vector3Rect(0f, 0f, -1f, 5f, 5f, 5f) });

            AssertValid(new MinModel { Value = new Vector3Circle(2f, 2f, 2f, 2f) });
            AssertInvalid<RuleIVector3MinAttribute>(
                new MinModel { Value = new Vector3Circle(2f, 2f, 1f, 2f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMax()
        {
            AssertValid(new MaxModel { Value = new Vector3Value(10f, 10f, 10f) });
            AssertInvalid<RuleIVector3MaxAttribute>(
                new MaxModel { Value = new Vector3Value(0f, 0f, 11f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMaxFixLowersZ()
        {
            var model = new MaxModel { Value = new Vector3Value(0f, 0f, 50f) };
            AssertFixed(model);

            Assert.AreEqual(10f, ((Vector3Value)model.Value).Z);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMaxRectStepAndCircle()
        {
            AssertValid(new MaxModel { Value = new Vector3RectStep(-5f, -5f, -5f, 10f, 10f, 10f, 1f) });
            AssertInvalid<RuleIVector3MaxAttribute>(
                new MaxModel { Value = new Vector3RectStep(-5f, -5f, -5f, 10f, 10f, 11f, 1f) });

            AssertValid(new MaxModel { Value = new Vector3Circle(8f, 8f, 8f, 2f) });
            AssertInvalid<RuleIVector3MaxAttribute>(
                new MaxModel { Value = new Vector3Circle(8f, 8f, 9f, 2f) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNull()
        {
            AssertInvalid<RuleIVector3InRangeAttribute>(new InRangeModel { Value = null });
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
