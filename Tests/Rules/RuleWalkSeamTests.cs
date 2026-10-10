using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Validations;
using NUnit.Framework;

namespace BH.SDK.Tests.Rules
{
    // Proves the seam and is the executable specification of what the generator must emit: property order,
    // phase A before phase B, descent gated by Check, reflection's rule array. BH.SDK/Docs/TESTING.md.


    /// <summary> The executable specification of what the validation generator must emit: a hand-written walk beside
    /// a structurally identical reflective model, producing the same findings, in the same order, with the
    /// same traces. </summary>
    [TestFixture]
    public class RuleWalkSeamTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void HandWrittenWalk_ReportsExactlyWhatReflectionReports()
        {
            var reflective = Report(BuildReflective());
            var generated = Report(BuildGenerated());

            TestContext.WriteLine(generated);
            Assert.That(generated, Is.EqualTo(reflective));
            Assert.That(generated, Is.Not.Empty, "the fixture stopped violating anything");
        }

        // A value carrying its own walk is reached through the SAME dispatch as everything else, so
        // one nested under a reflectively-walked owner still walks itself. That mutual recursion is
        // what makes the migration incremental - a half-generated model tree is a legal state.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AGeneratedNode_UnderAReflectiveOwner_StillWalksItself()
        {
            var owner = new Mixed { Value = 100, Child = BuildGenerated() };
            var report = Report(owner);

            Assert.That(report, Is.EqualTo(
                "rule_in_range Value\n"
                + "rule_in_range Child.Value\n"
                + "rule_string_max Child.Name\n"
                + "rule_in_range Child.Children[0].Value\n"));
        }

        // Generated is reached through IValidatable, never asked for [RuleContainer] (BHS1107 keeps it so).

        /// <summary> A model with no generated walk, reached through reflection. </summary>
        [RuleContainer]
        private class Reflective
        {
            [RuleInRange(0, 10)]
            public int Value { get; set; }

            [RuleStringMax(4)]
            public string Name { get; set; }

            public List<Reflective> Children { get; set; } = new();
        }

        /// <summary> The same model hand-written exactly as the generator is specified to emit it. </summary>
        [RuleContainer]
        private class Generated : IValidatable
        {
            [RuleInRange(0, 10)]
            public int Value { get; set; }

            [RuleStringMax(4)]
            public string Name { get; set; }

            public List<Generated> Children { get; set; } = new();

            private static readonly PropertyInfo ValueProperty = Property(nameof(Value));
            private static readonly PropertyInfo NameProperty = Property(nameof(Name));
            private static readonly PropertyInfo ChildrenProperty = Property(nameof(Children));

            private static readonly BasePropertyRuleAttribute[] ValueRules = Rules(ValueProperty);
            private static readonly BasePropertyRuleAttribute[] NameRules = Rules(NameProperty);
            private static readonly BasePropertyRuleAttribute[] ChildrenRules = Rules(ChildrenProperty);

            public void Validate(RuleWalk walk, RuleContext context)
            {
                // PHASE A - in GetProperties order, and all of it before anything descends.
                walk.Check(ValueProperty, ValueRules, Value, context);
                walk.Check(NameProperty, NameRules, Name, context);
                var descendChildren = walk.Check(ChildrenProperty, ChildrenRules, Children, context);

                // PHASE B.
                if (descendChildren) walk.DescendList(ChildrenProperty, Children, context);
            }

            private static PropertyInfo Property(string name)
                => typeof(Generated).GetProperty(name, BindingFlags.Public | BindingFlags.Instance);

            private static BasePropertyRuleAttribute[] Rules(PropertyInfo property)
                => property.GetCustomAttributes<BasePropertyRuleAttribute>(true).ToArray();
        }

        /// <summary> A reflectively walked owner holding a value that walks itself. </summary>
        [RuleContainer]
        private class Mixed
        {
            [RuleInRange(0, 10)]
            public int Value { get; set; }

            public Generated Child { get; set; }
        }

        // Both trees break the same rules at the same places: the root's Value, one over-long Name,
        // and one child whose Value is out of range - enough to exercise a leaf rule, a string rule,
        // a list descent and the descent gate at once.

        private static Reflective BuildReflective()
        {
            var root = new Reflective { Value = 100, Name = "far too long" };
            root.Children.Add(new Reflective { Value = 100, Name = "ok" });
            root.Children.Add(new Reflective { Value = 5, Name = "ok" });
            return root;
        }

        private static Generated BuildGenerated()
        {
            var root = new Generated { Value = 100, Name = "far too long" };
            root.Children.Add(new Generated { Value = 100, Name = "ok" });
            root.Children.Add(new Generated { Value = 5, Name = "ok" });
            return root;
        }

        private static string Report(object root)
        {
            var issues = new RuleAnalyzer().Analyze(root, new RuleAnalyzerSettings());

            var builder = new StringBuilder();
            foreach (var issue in issues)
            {
                builder.Append(issue.Rule.RuleNameKey).Append(' ')
                    .Append(issue.GetPath()).Append('\n');
            }
            return builder.ToString();
        }
    }
}
