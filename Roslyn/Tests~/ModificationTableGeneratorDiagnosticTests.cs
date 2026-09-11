using System.Linq;
using BH.SDK.Roslyn.Modification;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace BH.SDK.Roslyn.Tests
{
    // EVERY REFUSAL IS LOUD, for the same reason ModelGeneratorDiagnosticTests says it: the number
    // in a [ModificationField] is written by hand, and every way of writing it wrongly produces
    // code that compiles and behaves almost right. A duplicated id gives one override two fields
    // and reaches whichever the switch answers first; an unserialized member gives an override that
    // is recorded and never saved. Both are the silent failure this whole item exists to end.

    [TestFixture]
    public class ModificationTableGeneratorDiagnosticTests
    {
        private const string Usings = @"
using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Attributes;
using Newtonsoft.Json;
";

        private static GeneratorRun Run(string body)
            => GeneratorHarness.Run(new[] { new ModificationTableGenerator() }, Usings + body);

        private static void AssertOnly(GeneratorRun run, string id)
        {
            var reported = run.GeneratorDiagnostics.Select(d => d.Id).Distinct().ToList();

            Assert.That(reported, Is.EqualTo(new[] { id }),
                () => "reported: " + string.Join(", ", run.GeneratorDiagnostics.Select(d => d.ToString())));
            Assert.That(run.GeneratorDiagnostics.All(d => d.Severity == DiagnosticSeverity.Error),
                "a refusal is an error, never a warning that a build ignores");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TwoMembersClaimingOneId_AreRefused()
        {
            var run = Run(@"
namespace BH.SDK.Models.Objects
{
    public class RectObject
    {
        [ModificationField(ModificationFields.Layer)]
        [JsonProperty(""l"")]
        public int Layer { get; set; }

        [ModificationField(ModificationFields.Layer)]
        [JsonProperty(""name"")]
        public string Name { get; set; }
    }
}
");

            AssertOnly(run, "BHS1201");
            Assert.That(run.GeneratorDiagnostics.Single().GetMessage(), Does.Contain("RectObject.Layer"));
            Assert.That(run.GeneratorDiagnostics.Single().GetMessage(), Does.Contain("RectObject.Name"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AMemberWithNoJsonProperty_IsRefused()
        {
            var run = Run(@"
namespace BH.SDK.Models.Objects
{
    public class RectObject
    {
        [ModificationField(ModificationFields.Layer)]
        public int Layer { get; set; }
    }
}
");

            AssertOnly(run, "BHS1202");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        // A getter-only member would compile into a table whose Apply could never write it, which
        // is an override the editor records, shows as present, and drops.
        public void AMemberWithNoSetter_IsRefused()
        {
            var run = Run(@"
namespace BH.SDK.Models.Objects
{
    public class RectObject
    {
        [ModificationField(ModificationFields.Layer)]
        [JsonProperty(""l"")]
        public int Layer { get; }
    }
}
");

            AssertOnly(run, "BHS1202");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AZeroId_IsRefused()
        {
            var run = Run(@"
namespace BH.SDK.Models.Objects
{
    public class RectObject
    {
        [ModificationField(ModificationFields.None)]
        [JsonProperty(""l"")]
        public int Layer { get; set; }
    }
}
");

            AssertOnly(run, "BHS1203");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        // One band, one declaring type. Checked this way rather than against a hardcoded band map
        // so the generator holds no second copy of ModificationFields' header to keep in lockstep.
        public void ABandClaimedByTwoDeclaringTypes_IsRefused()
        {
            var run = Run(@"
namespace BH.SDK.Models.Objects
{
    public class RectObject
    {
        [ModificationField(ModificationFields.Layer)]
        [JsonProperty(""l"")]
        public int Layer { get; set; }
    }

    public sealed class ShapeObject : RectObject
    {
        [ModificationField(ModificationFields.Name)]
        [JsonProperty(""shid"")]
        public int ShapeId { get; set; }
    }
}
");

            AssertOnly(run, "BHS1203");
        }
    }
}
