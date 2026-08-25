using System.Diagnostics;
using System.Reflection;
using BH.SDK.Models;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Versions;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // Reading a level is the slowest thing a player or an editor does on the way into one, and the
    // envelope layer is where most of it was decided: every [DataVersion] domain resolves its own
    // concrete type from its own version, and how that lookup is done is the difference between one
    // streaming pass and one materialized JToken tree per domain. A Level nests ten domains and holds
    // one more per Prefab, so a tree per domain was a tree per prefab too.
    //
    // Both formats are measured because the choice between them is an authoring setting
    // (GameEditorSettings.Serialization.LevelMode), and "which is faster on a big level" should be a
    // number somebody can read rather than an assumption.

    /// <summary> Guards envelope read/write cost on a level the size of a real, heavy one. </summary>
    [TestFixture]
    public class SerializationPerformanceTests
    {
        private const int ObjectCount = 4750;
        private const int PrefabCount = 33;
        private const int PrefabObjectCount = 10;

        // Wall-clock budgets, deliberately loose - regression tripwires, not benchmark assertions.
        private const double JsonReadBudgetMs = 5000d;
        private const double BsonReadBudgetMs = 5000d;

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Extreme)]
        public void DeserializeEnvelope_LargeLevelJson_CompletesWithinBudget()
            => AssertRoundTripWithinBudget(SerializationType.Json, JsonReadBudgetMs);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Extreme)]
        public void DeserializeEnvelope_LargeLevelBson_CompletesWithinBudget()
            => AssertRoundTripWithinBudget(SerializationType.Bson, BsonReadBudgetMs);

        private static void AssertRoundTripWithinBudget(SerializationType type, double readBudgetMs)
        {
            var level = MockData.CreateLargeTestLevel(ObjectCount, PrefabCount, PrefabObjectCount);
            var service = new SerializationService();
            var serializer = service.GetDataSerializer(type);
            var attribute = typeof(Level).GetCustomAttribute<DataVersionAttribute>();

            var writeWatch = Stopwatch.StartNew();
            var bytes = serializer.SerializeEnvelope(attribute.Domain, new EnvelopeData(attribute.Version, level));
            writeWatch.Stop();

            // Warm-up read, untimed: the first pass through the converter graph pays every static
            // constructor, JIT stub and contract in it, which is not what this measures.
            serializer.DeserializeEnvelope(bytes, typeof(Level));

            var readWatch = Stopwatch.StartNew();
            var envelope = serializer.DeserializeEnvelope(bytes, typeof(Level));
            readWatch.Stop();

            var read = (Level)envelope.RawPayload;

            TestContext.WriteLine($"{type}: bytes={bytes.Length}");
            TestContext.WriteLine($"{type}: SerializeEnvelope={writeWatch.Elapsed.TotalMilliseconds:F1}ms");
            TestContext.WriteLine($"{type}: DeserializeEnvelope={readWatch.Elapsed.TotalMilliseconds:F1}ms");

            Assert.AreEqual(ObjectCount, read.Game.Objects.Count, "round trip lost objects");
            Assert.AreEqual(PrefabCount, read.Resources.Prefabs.Count, "round trip lost prefabs");

            Assert.That(readWatch.Elapsed.TotalMilliseconds, Is.LessThan(readBudgetMs),
                $"{type} DeserializeEnvelope took {readWatch.Elapsed.TotalMilliseconds:F0}ms on " +
                $"{bytes.Length} bytes ({ObjectCount} objects, {PrefabCount} prefabs)");
        }
    }
}
