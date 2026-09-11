using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// RuleStringMax: plain string length cap. Fix truncates rather than clearing, so an over-long
    /// name keeps its beginning instead of vanishing.
    /// </summary>
    public class RuleStringMaxTests : BaseRuleTests
    {
        /// <summary> A string with a length ceiling. </summary>
        [RuleContainer]
        private class Model
        {
            [RuleStringMax(5)] public string Value { get; set; } = string.Empty;
        }

        /// <summary> A ceiling of zero, where only the empty string passes. </summary>
        [RuleContainer]
        private class ZeroModel
        {
            [RuleStringMax(0)] public string Value { get; set; } = string.Empty;
        }

        /// <summary> A property of a type the rule does not apply to, so it must decline rather than refuse. </summary>
        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleStringMax(5)] public int Value { get; set; }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestValid()
        {
            AssertValid(new Model { Value = "abc" });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestEmpty()
        {
            AssertValid(new Model { Value = string.Empty });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestBoundary()
        {
            AssertValid(new Model { Value = "abcde" });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestJustOver()
        {
            AssertInvalid<RuleStringMaxAttribute>(new Model { Value = "abcdef" });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestFixTruncates()
        {
            var model = new Model { Value = "abcdefghij" };
            AssertFixedTo(model, () => model.Value, "abcde");
        }

        // A null string fails the shared BasePropertyRuleAttribute.IsValid null guard, not the length check -
        // and RuleStringMax's own Fix has nothing to truncate, so it stays broken. Pairing it with
        // RuleNotNull (as every live model does) is what actually repairs this.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestNullStaysBroken()
        {
            var model = new Model { Value = null };
            AssertInvalid<RuleStringMaxAttribute>(model);

            Fix(model);
            AssertInvalid<RuleStringMaxAttribute>(model);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TestZeroLength()
        {
            AssertValid(new ZeroModel { Value = string.Empty });
            AssertInvalid<RuleStringMaxAttribute>(new ZeroModel { Value = "a" });
        }

        // The ceiling counts CODE UNITS, and an astral character is two of them, so a cut landing
        // between the halves leaves a character that is not one. Truncation gives the shorter answer
        // rather than the broken one - the rule is already a Warning precisely because its repair is
        // destructive, and one code unit more destroyed is the cheaper of the two outcomes.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixNeverLeavesALoneSurrogate()
        {
            const string thumb = "👍"; // U+1F44D, one character, two code units

            // Five code units of ceiling, and the fifth lands inside the third pair.
            var model = new Model { Value = "ab" + thumb + thumb };
            AssertFixedTo(model, () => model.Value, "ab" + thumb);
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
