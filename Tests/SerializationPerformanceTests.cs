using System.Diagnostics;
using System.Reflection;
using BH.SDK.Models;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Versions;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // Reading a level is the slowest step into one; both formats are measured (LevelMode is an authoring
    // setting). Envelope resolution is one streaming pass, not a JToken tree per domain.

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

        // A RATIO, not a wall clock; 3 is far below measured, so only a structural break fails it.
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

        // The MINIMUM of several passes, never one: a single sample failed about one run in four.

        /// <summary> Milliseconds for the fastest of several warmed reads of one level in one
        /// format. </summary>
        private static double MeasureRead(Level level, SerializationType type)
        {
            var serializer = new SerializationService().GetDataSerializer(type);
            var attribute = typeof(Level).GetCustomAttribute<ModelGenerationAttribute>();
            var bytes = serializer.SerializeEnvelope(attribute.Domain, new EnvelopeData(attribute.Generation, level));

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
            var attribute = typeof(Level).GetCustomAttribute<ModelGenerationAttribute>();

            var writeWatch = Stopwatch.StartNew();
            var bytes = serializer.SerializeEnvelope(attribute.Domain, new EnvelopeData(attribute.Generation, level));
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