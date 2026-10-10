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
    // The collector separates this policy from the silent one it replaced: silent on an ordinary read,
    // present on a degraded one, free when nobody asked.

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
            // An unknown VALUE is the invisible half: a track fallen to its default looks authored that way.
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
            // The guard under every unknown-tag fixture; families are found as the format finds them.
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
