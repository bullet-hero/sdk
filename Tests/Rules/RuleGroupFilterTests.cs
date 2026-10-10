using System;
using System.Linq;
using System.Reflection;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Validations;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    // The filter that makes severity real. The Advice rule lives here: RuleCoverageTests scans only the
    // SDK assembly.

    /// <summary>
    /// RuleAnalyzerSettings.weakestGroup: which severities the analyzer reports.
    /// </summary>
    public class RuleGroupFilterTests : BaseRuleTests
    {
        /// <summary> A rule that always fails, at Advice severity - so the filter has something of every severity to sort. </summary>
        [AttributeUsage(BaseRuleAttribute.PropertyTarget)]
        private class AlwaysFailsAdviceAttribute : BasePropertyRuleAttribute
        {
            public override string RuleNameKey => "rule_always_fails_advice";
            public override RuleGroup Group => RuleGroup.Advice;
            public override bool HasFix => false;

            protected override bool IsValidTypeInternal(PropertyInfo property) => true;
            protected override bool IsValidInternal(object value, RuleContext context) => false;
            protected override void FixInternal(object target, PropertyInfo property, RuleContext context) { }
        }

        /// <summary> A model whose only violation is an Advice. </summary>
        [RuleContainer]
        private class AdviceModel
        {
            [AlwaysFailsAdvice]
            public int Value { get; set; }
        }

        /// <summary> A model whose only violation is an Error. </summary>
        [RuleContainer]
        private class ErrorModel
        {
            [RuleMinValue(10)]
            public int Value { get; set; }
        }

        /// <summary> A model violating rules of both severities at once. </summary>
        [RuleContainer]
        private class MixedModel
        {
            [AlwaysFailsAdvice]
            public int Advice { get; set; }

            [RuleMinValue(10)]
            public int Error { get; set; }
        }

        private static RuleAnalyzerSettings Reporting(RuleGroup weakestGroup)
            => new(true, true, weakestGroup);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestAdviceReportedByDefault()
        {
            CollectionAssert.IsNotEmpty(Analyze(new AdviceModel()));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestAdviceSuppressedAtWarning()
        {
            CollectionAssert.IsEmpty(Analyze(new AdviceModel(), Reporting(RuleGroup.Warning)));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestAdviceSuppressedAtError()
        {
            CollectionAssert.IsEmpty(Analyze(new AdviceModel(), Reporting(RuleGroup.Error)));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestErrorSurvivesEveryFilter()
        {
            CollectionAssert.IsNotEmpty(Analyze(new ErrorModel(), Reporting(RuleGroup.Error)));
            CollectionAssert.IsNotEmpty(Analyze(new ErrorModel(), Reporting(RuleGroup.Advice)));
        }

        // "Is this level playable at all" is exactly the Error-only query, and it must not be
        // drowned out by cosmetic findings on the same object.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestErrorOnlyReportKeepsErrorsDropsAdvice()
        {
            var issues = Analyze(new MixedModel(), Reporting(RuleGroup.Error));

            Assert.AreEqual(1, issues.Count);
            Assert.IsTrue(issues.All(issue => issue.Rule is RuleMinValueAttribute));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TestFullReportKeepsBoth()
        {
            var issues = Analyze(new MixedModel(), Reporting(RuleGroup.Advice));

            Assert.AreEqual(2, issues.Count);
        }
    }
}
