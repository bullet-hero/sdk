using System.Linq;
using BH.SDK.Roslyn.Model;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace BH.SDK.Roslyn.Tests
{
    // INCREMENTALITY IS NOT A NICETY HERE. Unity scopes this generator to BH.SDK and every assembly
    // referencing it, and BH.SDK is autoReferenced - so it is asked about roughly every assembly in
    // the project on every recompile, over 200 model types. If the pipeline cached by reference it
    // would re-emit all of them on every keystroke, and an editor that recompiles on save would
    // feel it.
    //
    // What makes it work is that ModelSpec holds no ISymbol - only strings and enums, with
    // EquatableArray for the member list, since ImmutableArray compares by reference. These two
    // fixtures are what notice if a symbol ever creeps back in.

    [TestFixture]
    public class ModelGeneratorIncrementalTests
    {
        private const string Source = @"
using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using Newtonsoft.Json;

namespace Fixture
{
    [GenerateModel]
    public sealed partial class Sample : IModel<Sample>
    {
        [JsonProperty(Names.Layer)] public int Layer { get; set; }
        public Sample() { }

        public int Twice() { return Layer * 2; }
    }
}";

        private static readonly string EditedBody = Source.Replace("Layer * 2", "Layer + Layer");
        private static readonly string EditedMembers = Source.Replace(
            "[JsonProperty(Names.Layer)] public int Layer { get; set; }",
            "[JsonProperty(Names.Layer)] public int Layer { get; set; }\n        [JsonProperty(Names.Name)] public string Name { get; set; }");

        private static bool AllCached(GeneratorRun run) => run.Result.Results
            .SelectMany(r => r.TrackedOutputSteps)
            .SelectMany(step => step.Value)
            .SelectMany(step => step.Outputs)
            .All(output => output.Reason == IncrementalStepRunReason.Cached);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void EditingAMethodBody_DoesNotReEmit()
        {
            var first = GeneratorHarness.Run(new[] { new ModelGenerator() }, Source);
            var second = GeneratorHarness.RunAgain(first, EditedBody);

            Assert.That(AllCached(second),
                "a body edit changes no member, so every generated source must come from the cache");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AddingAMember_DoesReEmit()
        {
            // The other half of the same claim: a cache that never invalidates is not a cache, it
            // is a stale file.
            var first = GeneratorHarness.Run(new[] { new ModelGenerator() }, Source);
            var second = GeneratorHarness.RunAgain(first, EditedMembers);

            Assert.That(AllCached(second), Is.False);
            Assert.That(second.Source("Fixture.Sample.Model.g.cs"), Does.Contain("Name"));
        }
    }
}
