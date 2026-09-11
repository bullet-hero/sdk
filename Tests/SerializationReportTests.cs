using System;
using System.Linq;
using BH.SDK.Models;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Converters.Base;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Versions;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // THE COLLECTOR IS WHAT SEPARATES THIS POLICY FROM THE ONE IT REPLACED. Degrading was never the
    // defect - the format did exactly that before, by Skip()ing a generation and reading an old
    // payload into today's class. What it did not do was SAY so, and a nested LevelSettings coming
    // back with fps=60 through one codec stack and fps=61 through the other is what that cost.
    //
    // So these are the three things the collector has to be: silent on an ordinary read, present on a
    // degraded one, and free when nobody asked.

    /// <summary> That a degraded read reports what it substituted, an ordinary one reports nothing, and a
    /// read with no collector costs nothing at all. </summary>
    [TestFixture]
    public class SerializationReportTests
    {
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AnOrdinaryRead_ReportsNothing()
        {
            var service = new SerializationService();
            var json = service.SerializeData(MockData.CreateTestLevel());

            var report = new SerializationReport();
            using (SerializationReport.Begin(report)) service.DeserializeData<Level>(json);

            Assert.IsTrue(report.IsEmpty, "an ordinary read substituted something: " + string.Join("; ", report.Entries));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void NoCollector_CostsNothingAndThrowsNothing()
        {
            // Report() is the only thing every degradation site calls, and it has to be safe to call
            // when nobody is collecting - which is every ordinary read the game ever does.
            Assert.IsNull(SerializationReport.Current);
            Assert.DoesNotThrow(() => SerializationReport.Report(
                ModelDomains.Level, "nothing", "nothing", ModelGenerations.Invalid,
                SubstitutionKind.UnknownTag));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void ACollector_IsRestoredWhenItsScopeEnds()
        {
            var outer = new SerializationReport();
            var inner = new SerializationReport();

            using (SerializationReport.Begin(outer))
            {
                Assert.AreSame(outer, SerializationReport.Current);

                using (SerializationReport.Begin(inner))
                    Assert.AreSame(inner, SerializationReport.Current);

                Assert.AreSame(outer, SerializationReport.Current);
            }

            Assert.IsNull(SerializationReport.Current);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void AnUnknownPolymorphicTag_StandsInTheFamilysLowestAndSaysSo()
        {
            // FloatType.Value is the family's lowest tag, and it is the family's plain literal form -
            // which is why the fallback needed nothing declared for it. An unknown VALUE is the
            // invisible half of this policy: a track that fell to its default looks like a level
            // authored that way, and only the report tells them apart.
            //
            // A polymorphic value is written `[tag, payload]`, so `"v":[0,` is every keyframe's own
            // value at its lowest tag. Retagging them all to a number nothing claims leaves the same
            // payloads in place - which is the point: the model comes back identical, and the ONLY
            // evidence that anything happened is the report.
            var service = new SerializationService();
            var json = service.SerializeData(MockData.CreateTestLevel())
                .Replace("\"v\":[0,", "\"v\":[" + MockData.FabricatedTag + ",");

            var report = new SerializationReport();
            Level level;
            using (SerializationReport.Begin(report)) level = service.DeserializeData<Level>(json);

            Assert.IsNotNull(level);
            Assert.That(report.Entries, Has.Some.Matches<SerializationSubstitution>(
                e => e.Kind == SubstitutionKind.UnknownTag));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void NoFamilyClaimsTheFabricatedTag()
        {
            // The guard under every unknown-tag fixture there is. The day a family reaches this tag,
            // all of them quietly become tests of an ORDINARY read that still pass - and stop
            // covering the thing they were written for. The families are found the way the format
            // itself finds them: the discriminator enum of every JsonConverterCustomType.
            var families = typeof(SerializationService).Assembly.GetTypes()
                .Select(t => t.BaseType)
                .Where(b => b is { IsGenericType: true }
                            && b.GetGenericTypeDefinition() == typeof(JsonConverterCustomType<,>))
                .Select(b => b.GetGenericArguments()[1])
                .Distinct()
                .ToArray();

            Assert.IsNotEmpty(families, "no polymorphic family was found - the reflection filter stopped matching");

            foreach (var family in families)
                Assert.IsFalse(Enum.IsDefined(family, Convert.ChangeType(
                        MockData.FabricatedTag, Enum.GetUnderlyingType(family))),
                    family.Name);
        }
    }
}
