using System.Linq;
using BH.SDK.Roslyn.Validation;
using NUnit.Framework;

namespace BH.SDK.Roslyn.Tests
{
    // WHAT THE VALIDATION GENERATOR EMITS, and - more to the point - what it REFUSES. The generated
    // walk itself is proven far more strongly elsewhere: BH.SDK.Tests and Core.Tests run a real
    // 19 341-object level through both walks and compare a hundred thousand findings, which no
    // assertion about emitted text could match. What that cannot reach is the diagnostics, because
    // every one of them describes a construct nobody has written yet.
    //
    // So this file is mostly refusals. Each names a way the generated property list could stop
    // being Type.GetProperties' list, in its order - and every one of those is silent: the same
    // findings under the wrong paths, in the wrong order, handed to a RuleFixer whose repairs are
    // not commutative.

    [TestFixture]
    public class ValidationGeneratorTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ALeafContainer_EmitsItsTableAndBothPhases()
        {
            var run = Run(@"
using BH.SDK.Rules.Attributes;
namespace Sample
{
    [RuleContainer]
    public sealed partial class Node
    {
        [RuleSample] public int Value { get; set; }
        [RuleSample] public Node Child { get; set; }
    }
}");

            var source = run.Source("Sample.Node.Validation.g.cs");

            Assert.That(source, Does.Contain("RuleTable.For(typeof(Node), false, new string[]"));
            Assert.That(source, Does.Contain("\"Node.Value\","));
            Assert.That(source, Does.Contain("\"Node.Child\","));
            Assert.That(source, Does.Contain("IValidatable.Validate("));

            // PHASE A BEFORE PHASE B, and the assertion is on their positions rather than on their
            // presence: an emitter that checked a property and immediately descended into it would
            // contain both of these lines too, in the order that reorders the report.
            var check = source.IndexOf("walk.Check(table, 1", System.StringComparison.Ordinal);
            var descend = source.IndexOf("walk.Descend", System.StringComparison.Ordinal);

            Assert.That(check, Is.GreaterThan(0));
            Assert.That(descend, Is.GreaterThan(check), "the descent was emitted before the checks");
        }

        // GetProperties is DERIVED-FIRST, and ModelEmitter's chain helpers call their base FIRST.
        // Copying that habit here inverts the report for every derived type and nothing stops
        // compiling, which is why the walk is emitted flat and why this asserts the order.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ADerivedContainer_ListsItsOwnPropertiesBeforeItsBases()
        {
            var run = Run(@"
using BH.SDK.Rules.Attributes;
namespace Sample
{
    [RuleContainer]
    public partial class Base
    {
        [RuleSample] public int FromBase { get; set; }
    }

    [RuleContainer]
    public sealed partial class Derived : Base
    {
        [RuleSample] public int FromDerived { get; set; }
    }
}");

            var source = run.Source("Sample.Derived.Validation.g.cs");
            var derived = source.IndexOf("\"Derived.FromDerived\"", System.StringComparison.Ordinal);
            var basis = source.IndexOf("\"Base.FromBase\"", System.StringComparison.Ordinal);

            Assert.That(derived, Is.GreaterThan(0));
            Assert.That(basis, Is.GreaterThan(derived),
                "the base's members were listed first - that is the opposite of GetProperties' order");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AnAbstractContainer_EmitsNothing()
        {
            var run = Run(@"
using BH.SDK.Rules.Attributes;
namespace Sample
{
    [RuleContainer]
    public abstract partial class Shape
    {
        [RuleSample] public int Value { get; set; }
    }
}");

            Assert.That(run.Sources.Keys, Has.No.Member("Sample.Shape.Validation.g.cs"),
                "an abstract type is never a runtime type, so the walk never reaches one");
        }

        // NOT A REFUSAL, AND DELIBERATELY SO. A container that is not partial keeps the reflective
        // walk, which is exactly what RuleWalk.Node's fallback is for - and BHS1101 was withdrawn
        // after erroring on dozens of the private nested fixtures in Tests/Rules, which are that
        // fallback's only coverage.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ANonPartialContainer_IsSkippedSilently()
        {
            var run = Run(@"
using BH.SDK.Rules.Attributes;
namespace Sample
{
    [RuleContainer]
    public sealed class Loose
    {
        [RuleSample] public int Value { get; set; }
    }
}");

            Assert.That(run.Sources, Is.Empty);
            Assert.That(run.GeneratorDiagnostics, Is.Empty);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AHiddenProperty_IsRefused()
        {
            var run = Run(@"
using BH.SDK.Rules.Attributes;
namespace Sample
{
    [RuleContainer]
    public partial class Base { [RuleSample] public int Value { get; set; } }

    [RuleContainer]
    public sealed partial class Derived : Base { [RuleSample] public new int Value { get; set; } }
}");

            Assert.That(Ids(run), Does.Contain("BHS1102"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AnOverrideDroppingAnAccessor_IsRefused()
        {
            var run = Run(@"
using BH.SDK.Rules.Attributes;
namespace Sample
{
    [RuleContainer]
    public abstract partial class Base { public virtual int Value { get; set; } }

    [RuleContainer]
    public sealed partial class Derived : Base { public override int Value { get { return 0; } } }
}");

            Assert.That(Ids(run), Does.Contain("BHS1103"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AnIndexer_IsRefused()
        {
            var run = Run(@"
using BH.SDK.Rules.Attributes;
namespace Sample
{
    [RuleContainer]
    public sealed partial class Bag
    {
        public int this[int index] { get { return index; } set { } }
    }
}");

            Assert.That(Ids(run), Does.Contain("BHS1104"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ANonPublicGetter_IsRefused()
        {
            var run = Run(@"
using BH.SDK.Rules.Attributes;
namespace Sample
{
    [RuleContainer]
    public sealed partial class Odd
    {
        [RuleSample] public int Value { private get { return 0; } set { } }
    }
}");

            Assert.That(Ids(run), Does.Contain("BHS1105"));
        }

        // The one refusal that is about a rule rather than about the property list: a rule on a
        // property the walk cannot reach has never run once and never will.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ARuleOnAGetOnlyProperty_IsRefused()
        {
            var run = Run(@"
using BH.SDK.Rules.Attributes;
namespace Sample
{
    [RuleContainer]
    public sealed partial class Computed
    {
        [RuleSample] public int Value { get { return 0; } }
    }
}");

            Assert.That(Ids(run), Does.Contain("BHS1108"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void NoAttribute_EmitsNothingAtAll()
        {
            var run = Run("namespace Sample { public sealed partial class Plain { public int Value { get; set; } } }");

            Assert.That(run.Sources, Is.Empty);
            Assert.That(run.GeneratorDiagnostics, Is.Empty);
        }

        private static GeneratorRun Run(string source)
            => GeneratorHarness.Run(new[] { new ValidationGenerator() }, ValidationApiStub, source);

        private static string Ids(GeneratorRun run)
            => string.Join(",", run.GeneratorDiagnostics.Select(diagnostic => diagnostic.Id));

        // Only as much of the validation API as the generator NAMES. It reasons about attributes,
        // property shapes and one interface; the walk it emits calls into RuleWalk, so the members
        // it calls have to exist for the emitted code to compile, but none of them has to do
        // anything - what the walk DOES is asserted against the real corpus, not here.
        private const string ValidationApiStub = @"
namespace BH.SDK.Rules
{
    public enum RuleGroup : byte { None = 0, Error = 1, Warning = 2, Advice = 3 }
    public sealed class RuleContext { }
}

namespace BH.SDK.Rules.Attributes
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class RuleContainerAttribute : System.Attribute { }

    public abstract class BaseRuleAttribute : System.Attribute { }
    public abstract class BasePropertyRuleAttribute : BaseRuleAttribute { }
    public abstract class BaseObjectRuleAttribute : BaseRuleAttribute { }

    [System.AttributeUsage(System.AttributeTargets.Property)]
    public sealed class RuleSampleAttribute : BasePropertyRuleAttribute { }
}

namespace BH.SDK.Validations
{
    public interface IValidatable
    {
        void Validate(RuleWalk walk, BH.SDK.Rules.RuleContext context);
    }

    public sealed class RuleTable
    {
        public System.Reflection.PropertyInfo[] Properties;

        public static RuleTable For(System.Type type, bool hasObjectRules, string[] expected)
            => new RuleTable();
    }

    public sealed class RuleWalk
    {
        public void ObjectRules(RuleTable table, object target, BH.SDK.Rules.RuleContext context) { }
        public bool Check(RuleTable table, int ordinal, object value, BH.SDK.Rules.RuleContext context) => false;
        public void Descend(System.Reflection.PropertyInfo p, object v, BH.SDK.Rules.RuleContext c) { }
        public void DescendOne(System.Reflection.PropertyInfo p, object v, BH.SDK.Rules.RuleContext c) { }
        public void DescendList(System.Reflection.PropertyInfo p, System.Collections.IList v, BH.SDK.Rules.RuleContext c) { }
        public void DescendArray(System.Reflection.PropertyInfo p, System.Array v, BH.SDK.Rules.RuleContext c) { }
        public void DescendDictionary(System.Reflection.PropertyInfo p, System.Collections.IDictionary v, BH.SDK.Rules.RuleContext c) { }
    }
}";
    }
}
