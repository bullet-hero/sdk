using NUnit.Framework;

namespace BH.SDK.Roslyn.Tests
{
    // Proves the rig itself works before anything is asserted through it: a driver runs, a
    // generator sees a real compilation, the emitted source lands, and the whole thing compiles.
    // It is pointed at SandboxProbeGenerator on purpose - the one generator whose only job is to
    // report what it saw, so a failure here is the harness and never the component under test.
    [TestFixture]
    public class HarnessSmokeTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TheProbeGenerator_EmitsItsStamp()
        {
            var run = GeneratorHarness.Run(new[] { new SandboxProbeGenerator() },
                "namespace Fixture { public class Anything { } }");

            var source = run.Source("RoslynSandboxStamp.g.cs");

            Assert.That(source, Does.Contain("internal static class RoslynSandboxStamp"));
            Assert.That(source, Does.Contain("public const string Assembly = \"BH.SDK.Generated.TestAssembly\";"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TheProbeGenerator_SawARealCompilation()
        {
            var run = GeneratorHarness.Run(new[] { new SandboxProbeGenerator() },
                "namespace Fixture { public class Anything { } }");

            // Two trees: the stubbed model API and the fixture above. A count of zero means the
            // harness handed the generator an empty compilation, which every assertion would then
            // pass against vacuously.
            Assert.That(run.Source("RoslynSandboxStamp.g.cs"), Does.Contain("SyntaxTrees = 2;"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TheStubbedModelApi_Compiles()
        {
            // The stub is what every other fixture is written against, so it has to be legal C# 9
            // on its own. A broken stub would otherwise surface as a confusing failure inside an
            // unrelated generator test.
            var run = GeneratorHarness.Run(new[] { new SandboxProbeGenerator() },
                @"
using BH.SDK.Models;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using Newtonsoft.Json;

namespace Fixture
{
    [GenerateModel]
    public partial class Sample : IModel<Sample>
    {
        [JsonProperty(Names.Layer)]
        public int Layer { get; set; }

        [JsonIgnore]
        public bool HasValue => Layer != 0;

        public object Clone() => Copy();
        public Sample Copy() => new Sample { Layer = Layer };
        public void Reset() { Layer = 0; }
        public void Update(Sample src) { Layer = src.Layer; }
        public void Pull(Sample src) { Layer = src.Layer; }
        public bool Equals(Sample other) => other is not null && Layer == other.Layer;
    }
}");

            Assert.That(run.CompilationErrors, Is.Empty,
                () => string.Join("\n", run.CompilationErrors));
        }
    }
}
