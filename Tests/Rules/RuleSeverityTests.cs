using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    // SEVERITY MUST BE CHOSEN, NEVER INHERITED BY SILENCE. BaseRuleAttribute answers Error, which is
    // the right default for a rule nobody has thought about and the wrong one for a rule nobody has
    // thought about YET - and for most of this project's life the difference was invisible, because
    // forty-four of forty-five rules took the default and ValidationReport.HasErrors was therefore
    // identical to !IsValid.
    //
    // So every rule either states its own Group or is named below as deliberately Error, and adding
    // a rule fails this test until one of the two is true. It is the same shape as RuleCoverageTests'
    // registry, for the same reason: a hand-kept list that has to be edited is what makes the
    // decision happen at all, and the diff is where it gets reviewed.

    [TestFixture]
    public class RuleSeverityTests
    {
        // DELIBERATELY ERROR: the file cannot be played as written. Ranges the runtime relies on,
        // finiteness, not-null, unique keyframe frames, valid enums and flags, collection bounds,
        // shape geometry, Min <= Max, identifiers. A rule here is not "unclassified" - it is
        // classified as fatal, and this list is where that was said.
        private static readonly HashSet<string> IntentionallyError = new()
        {
            nameof(RuleFiniteNumberAttribute),
            nameof(RuleInRangeAttribute),
            nameof(RuleMaxValueAttribute),
            nameof(RuleMinValueAttribute),
            nameof(RuleNotNullAttribute),
            nameof(RuleObjectIdValidAttribute),
            nameof(RuleParentObjectIdValidAttribute),
            nameof(RuleCollectionCountAttribute),
            nameof(RuleCollectionMaxCountAttribute),
            nameof(RuleCollectionMinCountAttribute),
            nameof(RuleCollectionNoNullItemsAttribute),
            nameof(RuleCollectionUniqueAttribute),
            nameof(RuleDictionaryKeyMatchesAttribute),
            nameof(RuleEnumValidAttribute),
            nameof(RuleEnumFlagsValidAttribute),
            nameof(RuleIFloatInRangeAttribute),
            nameof(RuleIFloatMaxAttribute),
            nameof(RuleIFloatMinAttribute),
            nameof(RuleIIntInRangeAttribute),
            nameof(RuleIVector2InRangeAttribute),
            nameof(RuleIVector2MaxAttribute),
            nameof(RuleIVector2MinAttribute),
            nameof(RuleIVector2OrderedAttribute),
            nameof(RuleIVector3InRangeAttribute),
            nameof(RuleIVector3MaxAttribute),
            nameof(RuleIVector3MinAttribute),
            nameof(RuleIVector4InRangeAttribute),
            nameof(RuleIVector4MaxAttribute),
            nameof(RuleIVector4MinAttribute),
            nameof(RuleIPrimitiveGuidNotNullAttribute),
            nameof(RuleIPrimitiveIntMaxAttribute),
            nameof(RuleIPrimitiveIntMinAttribute),
            nameof(RuleIPrimitiveIntNotNullAttribute),
            nameof(RuleStringPatternAttribute),
            nameof(RulePropertyOrderAttribute),
            nameof(RuleShapeGeometryAttribute),
        };

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void EveryRule_StatesItsSeverityOrIsNamedAsDeliberatelyError()
        {
            var undecided = new List<string>();

            foreach (var rule in Rules())
            {
                if (Declares(rule)) continue;
                if (IntentionallyError.Contains(rule.Name)) continue;

                undecided.Add(rule.Name);
            }

            Assert.That(undecided, Is.Empty,
                "these rules take BaseRuleAttribute's Error by default and nobody has said whether "
                + "that is right. Override Group, or add the name to IntentionallyError with the "
                + "reason in this file:\n  " + string.Join("\n  ", undecided));
        }

        // The registry is only worth its upkeep while it describes something real: a name left in it
        // after the rule started stating its own severity is a claim nobody checks any more.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TheDeliberatelyErrorList_HasNoStaleEntries()
        {
            var names = Rules().Select(rule => rule.Name).ToHashSet();
            var stale = IntentionallyError.Where(name => !names.Contains(name)).ToList();
            var overriding = Rules().Where(Declares).Select(r => r.Name)
                .Where(IntentionallyError.Contains).ToList();

            Assert.That(stale, Is.Empty, "no rule by these names exists:\n  "
                                         + string.Join("\n  ", stale));
            Assert.That(overriding, Is.Empty,
                "these state their own Group and no longer need to be listed:\n  "
                + string.Join("\n  ", overriding));
        }

        // WHAT THE DISTINCTION IS FOR. Until a rule reported anything but Error,
        // ValidationReport.HasErrors was the same predicate as !IsValid and no consumer could act on
        // it. This is what makes the two differ, and therefore what makes a "refuse to load on
        // errors" policy expressible at all.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void SeveritiesAreActuallyUsed()
        {
            var groups = Rules().Select(Severity).ToList();

            Assert.That(groups.Count(group => group == RuleGroup.Warning), Is.GreaterThan(0),
                "nothing reports Warning, so HasErrors is still identical to !IsValid");
            Assert.That(groups.Count(group => group == RuleGroup.Advice), Is.GreaterThan(0),
                "nothing reports Advice, so the weakestGroup filter has nothing to filter");
            Assert.That(groups, Has.No.Member(RuleGroup.None),
                "None means 'unclassified' and is always reported - no rule may claim it");

            TestContext.WriteLine(string.Join(", ", groups.GroupBy(group => group)
                .OrderBy(group => group.Key)
                .Select(group => group.Count() + " " + group.Key)));
        }

        private static IEnumerable<Type> Rules()
            => typeof(BaseRuleAttribute).Assembly.GetTypes()
                .Where(type => !type.IsAbstract && typeof(BaseRuleAttribute).IsAssignableFrom(type))
                .OrderBy(type => type.Name);

        /// <summary> Whether the rule states a Group of its own rather than inheriting one. </summary>
        private static bool Declares(Type rule)
        {
            var property = rule.GetProperty(nameof(BaseRuleAttribute.Group),
                BindingFlags.Public | BindingFlags.Instance);

            return property?.GetMethod?.DeclaringType == rule;
        }

        /// <summary> Read off an uninitialized instance, which is safe only because every Group in
        /// this project is a literal - the same thing RuleCoverageTests relies on for RuleNameKey. </summary>
        private static RuleGroup Severity(Type rule)
            => ((BaseRuleAttribute)System.Runtime.Serialization.FormatterServices
                .GetUninitializedObject(rule)).Group;
    }
}
