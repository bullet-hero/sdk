using System.Linq;
using BH.SDK.Roslyn.Model;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace BH.SDK.Roslyn.Tests
{
    // EVERY REFUSAL HAS TO BE LOUD, and that is not a style preference - it is the reason the
    // generator exists. The defect it replaces is silent by nature: a member present in Copy and
    // missing from Equals compiles and behaves almost right. So a member the generator cannot
    // express is an error naming the member, and these fixtures are what keep it from quietly
    // becoming a skip.

    [TestFixture]
    public class ModelGeneratorDiagnosticTests
    {
        private const string Usings = @"
using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using Newtonsoft.Json;
";

        private static GeneratorRun Run(string body)
            => GeneratorHarness.Run(new[] { new ModelGenerator() }, Usings + body);

        private static void AssertOnly(GeneratorRun run, string id)
        {
            var reported = run.GeneratorDiagnostics.Select(d => d.Id).Distinct().ToList();
            Assert.That(reported, Is.EqualTo(new[] { id }),
                () => "reported: " + string.Join(", ", run.GeneratorDiagnostics.Select(d => d.ToString())));
            Assert.That(run.GeneratorDiagnostics.All(d => d.Severity == DiagnosticSeverity.Error),
                "a refusal is an error, never a warning that a build ignores");
            Assert.That(run.Sources, Is.Empty, "a refused type must emit nothing at all");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ANonPartialType_IsRefused()
        {
            AssertOnly(Run(@"
namespace Fixture
{
    [GenerateModel]
    public sealed class Sample : IModel<Sample>
    {
        [JsonProperty(Names.Layer)] public int Layer { get; set; }
        public object Clone() => Copy();
        public Sample Copy() => new Sample();
        public void Reset() { }
        public void Update(Sample src) { }
        public void Pull(Sample src) { }
        public bool Equals(Sample other) => true;
    }
}"), "BHS1001");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ATypeWithNoParameterlessConstructor_IsRefused()
        {
            // Both Copy() and Reset() build one - Reset takes its defaults from what that
            // constructor writes rather than restating them - so its absence is fatal, not a
            // degraded mode.
            AssertOnly(Run(@"
namespace Fixture
{
    [GenerateModel]
    public sealed partial class Sample : IModel<Sample>
    {
        [JsonProperty(Names.Layer)] public int Layer { get; set; }
        public Sample(int layer) { Layer = layer; }
    }
}"), "BHS1002");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AMemberTheGeneratorCannotEncode_IsRefusedByName()
        {
            var run = Run(@"
namespace Fixture
{
    [GenerateModel]
    public sealed partial class Sample : IModel<Sample>
    {
        [JsonProperty(Names.Value)] public object Value { get; set; }
        public Sample() { }
    }
}");

            AssertOnly(run, "BHS1003");
            Assert.That(run.GeneratorDiagnostics.Single().GetMessage(), Does.Contain("Sample.Value"),
                "the message has to name the member, or nobody can act on it");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AnIgnoredMember_IsTheEscapeValve()
        {
            // The one thing a hook cannot do is un-emit a line, so a member with no encoding needs
            // an explicit opt-out - Modification.Value being the real case, an `object` whose
            // setter normalizes integrals to long.
            var run = GeneratorHarness.Run(new[] { new ModelGenerator() }, Usings + @"
namespace Fixture
{
    [GenerateModel]
    public sealed partial class Sample : IModel<Sample>
    {
        [GenerateModelIgnore]
        [JsonProperty(Names.Value)] public object Value { get; set; }

        [JsonProperty(Names.Layer)] public int Layer { get; set; }
        public Sample() { }
    }
}");

            Assert.That(run.GeneratorDiagnostics, Is.Empty);
            Assert.That(run.CompilationErrors, Is.Empty,
                () => string.Join("\n", run.CompilationErrors.Select(d => d.ToString())));

            // Named precisely rather than by the bare word: the codec is full of WriteValue calls,
            // and a test that cannot tell those from the member would pass for the wrong reason.
            var source = run.Source("Fixture.Sample.Model.g.cs");
            Assert.That(source, Does.Not.Contain("copy.Value"), "it must not be copied");
            Assert.That(source, Does.Not.Contain("WritePropertyName(\"value\")"), "it must not be written");
            Assert.That(source, Does.Not.Contain("case \"value\":"), "it must not be read");
            Assert.That(source, Does.Contain("copy.Layer"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AStruct_IsRefused()
        {
            AssertOnly(Run(@"
namespace Fixture
{
    [GenerateModel]
    public partial struct Sample : IModel<Sample>
    {
        [JsonProperty(Names.Layer)] public int Layer { get; set; }
    }
}"), "BHS1006");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void MergeOnSomethingThatIsNotADictionary_IsRefused()
        {
            AssertOnly(Run(@"
namespace Fixture
{
    [GenerateModel]
    public sealed partial class Sample : IModel<Sample>
    {
        [GenerateModelMerge]
        [JsonProperty(Names.Value)] public List<int> Values { get; set; }
        public Sample() { Values = new List<int>(); }
    }
}"), "BHS1005");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void APolymorphicModelWithNoDiscriminator_IsRefused()
        {
            // Reachable through the base, so a reader meets it without knowing what to construct.
            // Before this was a diagnostic it was a dispatcher that simply did not get emitted, and
            // the failure surfaced as a missing class at the one member that referenced it.
            //
            // The two TYPES are fine and are still emitted - what cannot be built is the dispatcher
            // between them, so this is the one refusal that is not "emit nothing".
            var run = Run(@"
namespace Fixture
{
    [GenerateModel]
    public partial class Node : IModel<Node>
    {
        [JsonProperty(Names.Layer)] public int Layer { get; set; }
        public Node() { }
    }

    [GenerateModel]
    public sealed partial class Child : Node, IModel<Child>
    {
        [JsonProperty(Names.Name)] public string Name { get; set; }
        public Child() { Name = string.Empty; }
    }

    [GenerateModel]
    public sealed partial class Holder : IModel<Holder>
    {
        // Declared as the BASE - which is what makes a discriminator necessary at all. Without a
        // member like this the hierarchy is never met polymorphically and nothing is demanded of
        // it, which is the case for most of the real ones (every keyframe member is a concrete
        // List<PosKey>, every audio effect its own field).
        [JsonProperty(Names.Value)] public Node Item { get; set; }
        public Holder() { }
    }
}");

            Assert.That(run.GeneratorDiagnostics.Select(d => d.Id).Distinct(),
                Is.EqualTo(new[] { "BHS1008" }),
                () => "reported: " + string.Join(", ", run.GeneratorDiagnostics.Select(d => d.ToString())));
            // One per untagged type - the base and its subtype are both unreadable without a tag,
            // and naming only the first would send the author round the loop twice.
            var named = run.GeneratorDiagnostics.Select(d => d.GetMessage()).ToList();
            Assert.That(named.Count, Is.EqualTo(2));
            Assert.That(named.Any(m => m.Contains("Node")));
            Assert.That(named.Any(m => m.Contains("Child")));
            // The PULL dispatcher needs no tag and is still written; only the blob half is refused.
            var dispatchers = run.Source("ModelDispatchers.g.cs");
            Assert.That(dispatchers, Does.Contain("NodeModelPull"));
            Assert.That(dispatchers, Does.Not.Contain("NodeBlob"),
                "a dispatcher it cannot build must not be half-built");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AGeneratedTypeUnderAHandWrittenModelBase_IsRefused()
        {
            // Both halves chain through base.Update/base.Pull/EqualsX, so a base that is a model
            // and is NOT generated leaves the derived body calling members nobody wrote.
            AssertOnly(Run(@"
namespace Fixture
{
    public partial class Node : IModel<Node>
    {
        public object Clone() => Copy();
        public Node Copy() => new Node();
        public void Reset() { }
        public void Update(Node src) { }
        public void Pull(Node src) { }
        public bool Equals(Node other) => true;
    }

    [GenerateModel]
    public sealed partial class Child : Node, IModel<Child>
    {
        [JsonProperty(Names.Layer)] public int Layer { get; set; }
        public Child() { }
    }
}"), "BHS1004");
        }
    }
}
