using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    /// <summary>
    /// RuleStringPattern: for strings that are lookup keys rather than prose, where being malformed
    /// means silently never matching instead of failing loudly.
    /// </summary>
    public class RuleStringPatternTests : BaseRuleTests
    {
        /// <summary> A language tag, which has a shape rather than a length. </summary>
        [RuleContainer]
        private class LanguageModel
        {
            [RuleStringPattern(ValueRules.LanguageCodePattern)]
            public string Value { get; set; } = ValueRules.DefaultLanguageCode;
        }

        /// <summary> The rule with no pattern given. </summary>
        [RuleContainer]
        private class DefaultModel
        {
            [RuleStringPattern(ValueRules.LanguageCodePattern, ValueRules.DefaultLanguageCode)]
            public string Value { get; set; } = ValueRules.DefaultLanguageCode;
        }

        /// <summary> A property of a type the rule does not apply to, so it must decline rather than refuse. </summary>
        [RuleContainer]
        private class WrongTypeModel
        {
            [RuleStringPattern("^a$")]
            public int Value { get; set; }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestPlainTag()
        {
            AssertValid(new LanguageModel { Value = "en" });
            AssertValid(new LanguageModel { Value = "ru" });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestRegionalAndScriptTags()
        {
            AssertValid(new LanguageModel { Value = "pt-BR" });
            AssertValid(new LanguageModel { Value = "zh-Hans-CN" });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestMalformedTags()
        {
            AssertInvalid<RuleStringPatternAttribute>(new LanguageModel { Value = "e" });
            AssertInvalid<RuleStringPatternAttribute>(new LanguageModel { Value = "en_US" });
            AssertInvalid<RuleStringPatternAttribute>(new LanguageModel { Value = "en-" });
            AssertInvalid<RuleStringPatternAttribute>(new LanguageModel { Value = "русский" });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestEmptyIsInvalid()
        {
            AssertInvalid<RuleStringPatternAttribute>(new LanguageModel { Value = string.Empty });
        }

        // Without a declared replacement there is nothing to repair to: any guess would turn a
        // visibly wrong key into an invisibly wrong one.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestNoDefaultStaysBroken()
        {
            var model = new LanguageModel { Value = "en_US" };
            Fix(model);

            Assert.AreEqual("en_US", model.Value);
            AssertInvalid<RuleStringPatternAttribute>(model);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFixUsesDeclaredDefault()
        {
            var model = new DefaultModel { Value = "en_US" };
            AssertFixedTo(model, () => model.Value, ValueRules.DefaultLanguageCode);
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
