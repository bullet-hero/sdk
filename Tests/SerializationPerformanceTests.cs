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
        private const double BlobReadBudgetMs = 5000d;

        // The claim the whole binary format makes, stated as a RATIO rather than a wall clock: a
        // number of milliseconds says as much about the machine as about the code, while "the
        // generated codec is several times faster than binding members by reflection" is the thing
        // that is actually true and stays true on a phone. Three is deliberately far below what is
        // measured, so this fails when something structural breaks and not when a laptop throttles.
        private const double MinimumBlobSpeedup = 3d;

        /// <summary> Timed passes per format in the ratio test - see MeasureRead. </summary>
        private const int MeasurePasses = 3;

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
        public void DeserializeEnvelope_LargeLevelBlob_CompletesWithinBudget()
            => AssertRoundTripWithinBudget(SerializationType.Blob, BlobReadBudgetMs);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Extreme)]
        public void DeserializeEnvelope_Blob_IsSeveralTimesFasterThanJson()
        {
            var level = MockData.CreateLargeTestLevel(ObjectCount, PrefabCount, PrefabObjectCount);

            var json = MeasureRead(level, SerializationType.Json);
            var blob = MeasureRead(level, SerializationType.Blob);

            TestContext.WriteLine($"json={json:F1}ms blob={blob:F1}ms speedup={json / blob:F1}x");

            Assert.That(json / blob, Is.GreaterThan(MinimumBlobSpeedup),
                $"blob read {blob:F0}ms against json {json:F0}ms - only {json / blob:F1}x, and the "
                + "point of a generated codec is that it is not in the same class as reflection");
        }

        // THE MINIMUM OF SEVERAL PASSES, NEVER ONE, and this is the difference between a tripwire and
        // a flaky test: one timed call catches whatever else the machine was doing during it, and a
        // ratio of two single samples catches it twice. A minimum is the right statistic here because
        // the thing being measured has a floor and no ceiling - every millisecond above the fastest
        // run is something other than the code. It ran as one pass each and failed roughly one run in
        // four on a busy machine, always on the ratio and never on the budgets.

        /// <summary> Milliseconds for the fastest of several warmed reads of one level in one
        /// format. </summary>
        private static double MeasureRead(Level level, SerializationType type)
        {
            var serializer = new SerializationService().GetDataSerializer(type);
            var attribute = typeof(Level).GetCustomAttribute<DataVersionAttribute>();
            var bytes = serializer.SerializeEnvelope(attribute.Domain, new EnvelopeData(attribute.Version, level));

            // Untimed: the first pass pays every static constructor, JIT stub and contract in the
            // graph, which is not what this measures.
            serializer.DeserializeEnvelope(bytes, typeof(Level));

            var best = double.MaxValue;
            for (var pass = 0; pass < MeasurePasses; pass++)
            {
                var watch = Stopwatch.StartNew();
                serializer.DeserializeEnvelope(bytes, typeof(Level));
                watch.Stop();

                if (watch.Elapsed.TotalMilliseconds < best) best = watch.Elapsed.TotalMilliseconds;
            }

            return best;
        }

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