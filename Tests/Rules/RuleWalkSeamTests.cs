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
    // PROVES THE SEAM BEFORE THE GENERATOR EXISTS. RuleWalk.Node dispatches a value carrying an
    // IValidatable to its own walk and everything else to reflection, and the whole of phase 3 rests
    // on those two producing the SAME report - same findings, same order, same traces. Nothing in
    // the SDK implements IValidatable yet, so this fixture hand-writes one exactly as the generator
    // is specified to emit it, beside a structurally identical model that has none, and compares.
    //
    // It is therefore also the executable specification of what BH.SDK.Roslyn must emit, and every
    // trap it encodes is one that would silently change a real level's report:
    //
    // - PROPERTY ORDER IS GetProperties' ORDER, which is declaration order per level and
    //   derived-first across levels. Checking them in any other order reorders the report.
    // - PHASE A IS TOTAL BEFORE PHASE B BEGINS. Every property's own rules run first; only then does
    //   anything descend. Fusing the two is the natural way to write this by hand and it reorders
    //   the report for every object with more than one walkable property.
    // - THE DESCENT IS GATED BY Check's RETURN. A failing rule suppresses the walk into that value
    //   (unless analyzeAllRecursiveRules), and dropping that gate reports findings the reflective
    //   path never would.
    // - THE RULE ARRAY COMES FROM REFLECTION, in reflection's own order, holding reflection's own
    //   instances. Under the default analyzeAllPropertyRules = false the FIRST failing rule wins and
    //   the loop breaks, so a different array order reports a different finding - and RuleIssue.Rule
    //   is expected to be the same object for every node of a type, which fresh attribute instances
    //   would break.

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

        // Neither model is walked as a [RuleContainer] by declaration alone - Generated is reached
        // through IValidatable and never asked. That asymmetry is deliberate and is what BHS1107
        // exists to keep honest: implementing the interface by hand opts a type out of the container
        // check entirely.

        [RuleContainer]
        private class Reflective
        {
            [RuleInRange(0, 10)]
            public int Value { get; set; }

            [RuleStringMax(4)]
            public string Name { get; set; }

            public List<Reflective> Children { get; set; } = new();
        }

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
