using System.Linq;
using BH.SDK.Roslyn.Modification;
using NUnit.Framework;

namespace BH.SDK.Roslyn.Tests
{
    // THE STRONGEST ASSERTION HERE IS "IT COMPILES", exactly as in ModelGeneratorTests: a table
    // whose switch names a member that is not there, or converts to a type the member does not
    // hold, is a compile error in the generated file rather than a wrong answer at run time. The
    // text assertions below check the SHAPE decisions - the type test only where it is needed, the
    // index refused on a scalar, the whole-list and element branches - and every one of them is
    // checked after the compilation has already been asserted clean.

    [TestFixture]
    public class ModificationTableGeneratorTests
    {
        private const string Usings = @"
using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Attributes;
using Newtonsoft.Json;
";

        // The two-type hierarchy the real models have, cut to what a table needs: a root carrying an
        // inherited member and one subclass carrying its own. Everything about inheritance the
        // generator decides is decided on this shape.
        private const string Models = @"
namespace BH.SDK.Models.Objects
{
    public class RectObject
    {
        [ModificationField(ModificationFields.Name)]
        [JsonProperty(""name"")]
        public string Name { get; set; }

        [ModificationField(ModificationFields.Layer)]
        [JsonProperty(""l"")]
        public int Layer { get; set; }

        [ModificationField(ModificationFields.Positions)]
        [JsonProperty(""pos"")]
        public List<int> Positions { get; set; }
    }

    public sealed class ShapeObject : RectObject
    {
        [ModificationField(ModificationFields.ShapeId)]
        [JsonProperty(""shid"")]
        public int ShapeId { get; set; }
    }
}
";

        private static GeneratorRun Run(string body)
            => GeneratorHarness.Run(new[] { new ModificationTableGenerator() }, Usings + body);

        private static void AssertCompiles(GeneratorRun run)
        {
            Assert.That(run.CompilationErrors, Is.Empty,
                () => string.Join("\n", run.CompilationErrors.Select(error => error.ToString()))
                      + "\n\n" + string.Join("\n\n", run.Sources.Select(pair => pair.Key + ":\n" + pair.Value)));
        }

        private static string Table(GeneratorRun run) => run.Source("ModificationTable.g.cs");

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void TheTable_Compiles()
        {
            var run = Run(Models);

            Assert.That(run.GeneratorDiagnostics, Is.Empty,
                () => string.Join("\n", run.GeneratorDiagnostics.Select(d => d.ToString())));
            AssertCompiles(run);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        // The whole point of keying on the DECLARATION: an inherited member is one case with no
        // type test, and the subclass's own is one case with exactly one.
        public void AnInheritedMember_NeedsNoTypeTest()
        {
            var table = Table(Run(Models));

            Assert.That(table, Does.Contain("case 0x0104:"));
            Assert.That(table, Does.Contain("obj.Layer = converted;"));
            Assert.That(table, Does.Not.Contain("is global::BH.SDK.Models.Objects.RectObject target"));

            Assert.That(table, Does.Contain("case 0x0201:"));
            Assert.That(table, Does.Contain("if (!(obj is global::BH.SDK.Models.Objects.ShapeObject target)) return false;"));
            Assert.That(table, Does.Contain("target.ShapeId = converted;"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        // An index is legal on a collection and on nothing else, which is the invariant the
        // validation rule reads back through IsCollection.
        public void AnIndex_IsLegalOnACollectionOnly()
        {
            var run = Run(Models);
            var table = Table(run);

            Assert.That(table, Does.Contain("if (index >= 0) return false;"));
            Assert.That(table, Does.Contain("list[index] = element;"));
            Assert.That(table, Does.Contain("if (list is null || index >= list.Count) return false;"));

            var isCollection = table.Substring(table.IndexOf("public static bool IsCollection", System.StringComparison.Ordinal));
            Assert.That(isCollection, Does.Contain("case 0x0106:"));
            Assert.That(isCollection.Substring(0, isCollection.IndexOf("return false;", System.StringComparison.Ordinal)),
                Does.Not.Contain("case 0x0104:"), "A scalar answered IsCollection, so an index would be accepted on it.");

            AssertCompiles(run);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        // PropertyOf is the one reflective member and exists only for the rule-checked write. Each
        // entry is a literal typeof/nameof pair so an IL2CPP strip cannot take the member away.
        public void PropertyOf_IsLiteralAndLazy()
        {
            var table = Table(Run(Models));

            Assert.That(table, Does.Contain(
                "typeof(global::BH.SDK.Models.Objects.RectObject).GetProperty(nameof(global::BH.SDK.Models.Objects.RectObject.Layer))"));
            Assert.That(table, Does.Contain("if (properties is null)"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        // A compilation holding no marked member emits nothing at all, rather than an empty table
        // that every downstream assembly would then have to reference.
        public void ACompilationWithNoMarkedMember_EmitsNothing()
        {
            var run = GeneratorHarness.Run(new[] { new ModificationTableGenerator() });

            Assert.That(run.Sources, Is.Empty);
            Assert.That(run.GeneratorDiagnostics, Is.Empty);
        }
    }
}
