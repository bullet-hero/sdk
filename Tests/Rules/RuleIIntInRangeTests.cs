using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// RuleIIntInRange over all three IInt variants. Only LayerKey.Layer uses it today, bounding an
    /// animated draw-order track to the authored layer band.
    /// </summary>
    public class RuleIIntInRangeTests : BaseRuleTests
    {
        /// <summary> The model the rule under test sits on. </summary>
        [RuleContainer]
        private class Model
        {
            [RuleIIntInRange(-10, 10)]
            public IInt Value { get; set; } = new IntValue(0);
        }

        /// <summary> A property of a type the rule does not apply to, so it must decline rather than refuse. </summary>
        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleIIntInRange(-10, 10)]
            public int Value { get; set; }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValueValid()
        {
            AssertValid(new Model { Value = new IntValue(0) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValueBoundaries()
        {
            AssertValid(new Model { Value = new IntValue(-10) });
            AssertValid(new Model { Value = new IntValue(10) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestValueOutOfRange()
        {
            AssertInvalid<RuleIIntInRangeAttribute>(new Model { Value = new IntValue(-11) });
            AssertInvalid<RuleIIntInRangeAttribute>(new Model { Value = new IntValue(11) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixValueClamps()
        {
            var model = new Model { Value = new IntValue(500) };
            AssertFixedTo(model, () => ((IntValue)model.Value).Value, 10);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMinMax()
        {
            AssertValid(new Model { Value = new IntMinMax(-5, 5) });
            AssertInvalid<RuleIIntInRangeAttribute>(new Model { Value = new IntMinMax(-50, 5) });
            AssertInvalid<RuleIIntInRangeAttribute>(new Model { Value = new IntMinMax(-5, 50) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixMinMaxClampsBothEnds()
        {
            var model = new Model { Value = new IntMinMax(-50, 50) };
            AssertFixed(model);

            var value = (IntMinMax)model.Value;
            Assert.AreEqual(-10, value.Min);
            Assert.AreEqual(10, value.Max);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMinMaxStep()
        {
            AssertValid(new Model { Value = new IntMinMaxStep(-5, 5, 1) });
            AssertInvalid<RuleIIntInRangeAttribute>(new Model { Value = new IntMinMaxStep(-5, 50, 1) });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNull()
        {
            AssertInvalid<RuleIIntInRangeAttribute>(new Model { Value = null });
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
