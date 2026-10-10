using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Validations;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    // One file per [RuleXxx] with throwaway [RuleContainer] fixtures, not MockData, so a failure names one
    // rule. A fixture needs only [RuleContainer] and public get/set properties.

    /// <summary>
    /// Shared harness for the per-attribute rule tests: analyze/fix plumbing plus assertions that
    /// report which rule fired and where, instead of a bare "collection was not empty".
    /// </summary>
    public abstract class BaseRuleTests
    {
        // One analyzer for the whole run - its constructor does a full reflection scan of the SDK
        // assembly, and it keeps no state between Analyze calls (trace cleared, pool returned).
        private static readonly RuleAnalyzer Analyzer = new();
        private static readonly RuleFixer Fixer = new();

        /// <summary> Settings that report every rule of every property, instead of stopping at the
        /// first failure - a per-attribute test wants the complete picture. </summary>
        protected static RuleAnalyzerSettings AllRules => new(true, true);

        protected static List<RuleIssue> Analyze(object model)
            => Analyzer.Analyze(model, AllRules);

        protected static List<RuleIssue> Analyze(object model, RuleAnalyzerSettings settings)
            => Analyzer.Analyze(model, settings);

        /// <summary> Model passes every rule on it. </summary>
        protected static void AssertValid(object model)
        {
            var issues = Analyze(model);
            Assert.IsEmpty(issues, $"Expected no issues, got:\n{Describe(issues)}");
        }

        /// <summary> Model breaks exactly one rule, and it is TRule. </summary>
        protected static void AssertInvalid<TRule>(object model) where TRule : BaseRuleAttribute
            => AssertInvalid<TRule>(model, 1);

        /// <summary> Model breaks TRule exactly expectedCount times and breaks nothing else. </summary>
        protected static void AssertInvalid<TRule>(object model, int expectedCount)
            where TRule : BaseRuleAttribute
        {
            var issues = Analyze(model);
            var matched = issues.Count(issue => issue.Rule is TRule);

            Assert.AreEqual(expectedCount, matched,
                $"Expected {expectedCount} {typeof(TRule).Name} issue(s), got {matched}:\n{Describe(issues)}");
            Assert.AreEqual(expectedCount, issues.Count,
                $"Expected only {typeof(TRule).Name} issues, got:\n{Describe(issues)}");
        }

        /// <summary> Model breaks TRule at least once. Use when the fixture is a real aggregate that
        /// may legitimately raise other issues too; prefer AssertInvalid everywhere else. </summary>
        protected static void AssertHasIssue<TRule>(object model) where TRule : BaseRuleAttribute
        {
            var issues = Analyze(model);

            Assert.IsTrue(issues.Any(issue => issue.Rule is TRule),
                $"Expected at least one {typeof(TRule).Name} issue, got:\n{Describe(issues)}");
        }

        /// <summary> Run RuleFixer over whatever the analyzer currently reports. Use directly only
        /// when asserting that a fix path deliberately does nothing - AssertFixed covers the rest. </summary>
        protected static void Fix(object model)
        {
            var issues = Analyze(model);
            Fixer.Fix(issues, new RuleFixerSettings());
        }

        /// <summary> Model is currently invalid, and RuleFixer makes it fully valid. </summary>
        protected static void AssertFixed(object model)
        {
            var issues = Analyze(model);
            Assert.IsNotEmpty(issues, "Expected the model to be invalid before fixing");

            Fixer.Fix(issues, new RuleFixerSettings());

            issues = Analyze(model);
            Assert.IsEmpty(issues, $"Still invalid after fixing:\n{Describe(issues)}");
        }

        /// <summary> Fix produces the exact expected value, not merely a valid one - a clamp that
        /// silently lands on the wrong bound still passes AssertFixed. </summary>
        protected static void AssertFixedTo<TValue>(object model, Func<TValue> actual, TValue expected)
        {
            AssertFixed(model);
            Assert.AreEqual(expected, actual(),
                $"Fix produced a valid but unexpected value for {model.GetType().Name}");
        }

        /// <summary> Attribute sits on a type it cannot handle - RuleAnalyzer must reject it loudly
        /// rather than silently passing the property. </summary>
        protected static void AssertWrongType(object model)
        {
            Assert.Throws<ArgumentException>(() => Analyze(model),
                $"Expected {model.GetType().Name} to fail IsValidType");
        }

        /// <summary> Severity a rule reports for its issues. </summary>
        protected static void AssertGroup<TRule>(object model, RuleGroup expected)
            where TRule : BaseRuleAttribute
        {
            var issues = Analyze(model);
            var issue = issues.FirstOrDefault(i => i.Rule is TRule);

            Assert.IsNotNull(issue.Rule, $"No {typeof(TRule).Name} issue raised:\n{Describe(issues)}");
            Assert.AreEqual(expected, issue.Rule.Group);
        }

        private static string Describe(IReadOnlyList<RuleIssue> issues)
        {
            if (issues.Count == 0) return "  <none>";

            var builder = new StringBuilder();
            foreach (var issue in issues)
            {
                builder.Append("  ").Append(issue.Rule.GetType().Name)
                    .Append(" at ").Append(issue.GetPath()).AppendLine();
            }
            return builder.ToString();
        }
    }
}
