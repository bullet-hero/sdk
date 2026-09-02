using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BH.SDK.Models;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Services.Cache;
using BH.SDK.Services.Content;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // =========================================================================================
    // TESTS FOR DEAD CODE, KEPT ON PURPOSE - A DELETION CANDIDATE alongside what they cover.
    //
    // The level cache they exercise is disconnected from the game (see `LevelCachePool`'s own
    // banner). They still run and must still pass: while the codec is in the tree it stays
    // honest, and the shapes asserted here - a round trip that ends in `Level.Equals`, a
    // truncation sweep, a refused generation, refused garbage - are the shapes the `.blob`
    // format's own tests should take. See docs/issues/ROSLYN_PLAN.md.
    // =========================================================================================

    // THE ROUND TRIP IS THE WHOLE CONTRACT. The cache hands a `Level` to a player instead of the one
    // the file says, so the only thing that makes it safe is that the two are equal - and `Level`
    // implements `Equals` deeply for exactly this kind of question. Everything else here is about
    // the cache DECLINING, which is the other half: a payload it cannot reproduce must never be
    // half-read into a level nobody authored.

    [TestFixture]
    public class LevelCacheTests
    {
        // Big enough that the codec meets every branch it has (four object types, prefab templates,
        // per-instance modifications) and small enough to stay in the ordinary lane.
        private const int ObjectCount = 500;
        private const int PrefabCount = 8;
        private const int PrefabObjectCount = 6;

        private static SerializationService Serialization() => new(new SerializationSettings());

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void APayload_ReadsBackAsTheSameLevel()
        {
            var serialization = Serialization();
            var level = MockData.CreateTestLevel();

            var payload = LevelCacheCodec.Write(serialization, level);

            Assert.IsTrue(LevelCacheCodec.TryRead(serialization, payload, out var read));
            Assert.IsTrue(level.Equals(read), "A cached level is not the level that was cached");
        }

        // The half the ordinary fixture cannot reach: prefab templates carrying objects of their
        // own, which go through the same codec rather than through the BSON half.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void ALevelOfEveryShape_ReadsBackAsTheSameLevel()
        {
            var serialization = Serialization();
            var level = MockData.CreateLargeTestLevel(ObjectCount, PrefabCount, PrefabObjectCount);

            var payload = LevelCacheCodec.Write(serialization, level);

            Assert.IsTrue(LevelCacheCodec.TryRead(serialization, payload, out var read));
            Assert.IsTrue(level.Equals(read));
            Assert.AreEqual(level.Game.Objects.Count, read.Game.Objects.Count);
            Assert.AreEqual(level.Resources.Prefabs.Count, read.Resources.Prefabs.Count);
        }

        // The objects are LIFTED OUT of the level to serialize the rest of it, and put back in a
        // `finally`. A caller handing over the level it is about to play must get it back whole.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Writing_LeavesTheSourceLevelUntouched()
        {
            var serialization = Serialization();
            var level = MockData.CreateLargeTestLevel(ObjectCount, PrefabCount, PrefabObjectCount);
            var before = level.Copy();

            LevelCacheCodec.Write(serialization, level);

            Assert.IsTrue(before.Equals(level), "Writing a cache payload mutated the level");
            Assert.AreEqual(ObjectCount, level.Game.Objects.Count);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Garbage_IsRefusedRatherThanThrown()
        {
            var serialization = Serialization();

            Assert.IsFalse(LevelCacheCodec.TryRead(serialization, null, out _));
            Assert.IsFalse(LevelCacheCodec.TryRead(serialization, Array.Empty<byte>(), out _));
            Assert.IsFalse(LevelCacheCodec.TryRead(serialization, new byte[64], out _));

            var random = new byte[4096];
            new Random(1).NextBytes(random);
            Assert.IsFalse(LevelCacheCodec.TryRead(serialization, random, out _));
        }

        // A payload cut short is the ordinary way a cache goes wrong - a write interrupted by a
        // process going away. Every prefix of a real payload has to be refused, not partly read.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void ATruncatedPayload_IsRefused()
        {
            var serialization = Serialization();
            var payload = LevelCacheCodec.Write(serialization, MockData.CreateTestLevel());

            for (var length = 1; length < payload.Length; length += Math.Max(1, payload.Length / 40))
            {
                var truncated = new byte[length];
                Array.Copy(payload, truncated, length);

                Assert.IsFalse(LevelCacheCodec.TryRead(serialization, truncated, out _),
                    $"A payload truncated to {length} bytes was accepted");
            }
        }

        // The codec generation is the one thing standing between a model change and a level decoded
        // into the wrong shape, so it is checked before anything else is read.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void AnotherCodecGeneration_IsRefused()
        {
            var serialization = Serialization();
            var payload = LevelCacheCodec.Write(serialization, MockData.CreateTestLevel());

            // The version is the second field, right after the four magic bytes.
            payload[4]++;

            Assert.IsFalse(LevelCacheCodec.TryRead(serialization, payload, out _));
        }

        #region Keys

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void AKeyWithNoSource_IsNotValid()
        {
            Assert.IsFalse(default(LevelCacheKey).IsValid);
            Assert.IsFalse(new LevelCacheKey(null, 10, 1).IsValid);
            Assert.IsFalse(new LevelCacheKey(string.Empty, 10, 1).IsValid);
            Assert.IsFalse(new LevelCacheKey("levels/a", 0, 1).IsValid);
            Assert.IsTrue(new LevelCacheKey("levels/a", 10, 1).IsValid);
        }

        // Each of the three moving parts has to be enough on its own to make the cache decline -
        // that is what makes an edit that kept the size, or a rebuild that kept the timestamp, a
        // miss rather than a wrong level.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void AnyPartMoving_MakesADifferentKey()
        {
            var key = new LevelCacheKey("levels/a", 10, 1);

            Assert.AreEqual(key, new LevelCacheKey("levels/a", 10, 1));
            Assert.AreNotEqual(key, new LevelCacheKey("levels/b", 10, 1));
            Assert.AreNotEqual(key, new LevelCacheKey("levels/a", 11, 1));
            Assert.AreNotEqual(key, new LevelCacheKey("levels/a", 10, 2));
            Assert.AreNotEqual(key, new LevelCacheKey("levels/a", 10, 1, LevelCacheFormat.Version + 1));
        }

        #endregion

        #region Pool

        private static LevelCachePool Pool(out MemoryContentStore store)
        {
            store = new MemoryContentStore();
            return new LevelCachePool(Serialization(), store);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task APooledLevel_ComesBackFromEitherTier()
        {
            var pool = Pool(out var store);
            var key = new LevelCacheKey("levels/a", 100, 7);
            var level = MockData.CreateTestLevel();

            await pool.PutAsync(key, level, CancellationToken.None);

            Assert.IsTrue(pool.TryGet(key, out var fromMemory));
            Assert.IsTrue(level.Equals(fromMemory));

            // The store tier answers on its own, which is what the NEXT session actually uses.
            var cold = new LevelCachePool(Serialization(), store);
            Assert.IsFalse(cold.TryGet(key, out _));

            var fromStore = await cold.GetAsync(key, CancellationToken.None);
            Assert.IsNotNull(fromStore);
            Assert.IsTrue(level.Equals(fromStore));
        }

        // Every consumer of a level mutates it, so two callers must never be handed the same graph.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task TwoReads_AreTwoInstances()
        {
            var pool = Pool(out _);
            var key = new LevelCacheKey("levels/a", 100, 7);

            await pool.PutAsync(key, MockData.CreateTestLevel(), CancellationToken.None);

            Assert.IsTrue(pool.TryGet(key, out var first));
            Assert.IsTrue(pool.TryGet(key, out var second));

            Assert.IsFalse(ReferenceEquals(first, second));
            Assert.IsFalse(ReferenceEquals(first.Game, second.Game));
            Assert.IsTrue(first.Equals(second));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task AStaleKey_Misses()
        {
            var pool = Pool(out _);
            var key = new LevelCacheKey("levels/a", 100, 7);

            await pool.PutAsync(key, MockData.CreateTestLevel(), CancellationToken.None);

            Assert.IsFalse(pool.TryGet(new LevelCacheKey("levels/a", 101, 7), out _));
            Assert.IsFalse(pool.TryGet(new LevelCacheKey("levels/a", 100, 8), out _));
            Assert.IsNull(await pool.GetAsync(new LevelCacheKey("levels/a", 100, 8), CancellationToken.None));
        }

        // Invalidation drops EVERY version written for a source, since the caller that saved a level
        // no longer knows the length and stamp of the payload it is replacing.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task Invalidating_DropsEveryVersionOfASource()
        {
            var pool = Pool(out var store);
            var level = MockData.CreateTestLevel();

            var first = new LevelCacheKey("levels/a", 100, 7);
            var second = new LevelCacheKey("levels/a", 140, 9);
            var other = new LevelCacheKey("levels/b", 100, 7);

            await pool.PutAsync(first, level, CancellationToken.None);
            await pool.PutAsync(second, level, CancellationToken.None);
            await pool.PutAsync(other, level, CancellationToken.None);

            await pool.InvalidateAsync("levels/a", CancellationToken.None);

            var cold = new LevelCachePool(Serialization(), store);
            Assert.IsNull(await cold.GetAsync(first, CancellationToken.None));
            Assert.IsNull(await cold.GetAsync(second, CancellationToken.None));
            Assert.IsNotNull(await cold.GetAsync(other, CancellationToken.None),
                "Invalidating one source took another one's payload with it");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task APoolWithNoStore_StillAnswersWithinTheSession()
        {
            var pool = new LevelCachePool(Serialization());
            var key = new LevelCacheKey("levels/a", 100, 7);

            Assert.IsFalse(pool.HasStore);

            await pool.PutAsync(key, MockData.CreateTestLevel(), CancellationToken.None);
            Assert.IsTrue(pool.TryGet(key, out _));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public async Task TheMemoryTier_StaysWithinItsBudget()
        {
            var pool = new LevelCachePool(Serialization(), null, maxEntries: 2);
            var level = MockData.CreateTestLevel();

            for (var i = 0; i < 5; i++)
                await pool.PutAsync(new LevelCacheKey("levels/a", 100 + i, 7), level, CancellationToken.None);

            Assert.LessOrEqual(pool.Count, 2);

            // The one just put is the one being opened, so it is the one that must still be there.
            Assert.IsTrue(pool.TryGet(new LevelCacheKey("levels/a", 104, 7), out _));
        }

        #endregion

        // THE REASON THIS EXISTS AT ALL, asserted as a RATIO rather than a wall clock: the claim is
        // "decoding is a different order of work from parsing", and a ratio says that on any machine
        // while an absolute budget only says it on this one. The margin is enormous on purpose -
        // measured well past 10x on a real level, so 2x failing means something structural broke,
        // not that the run was noisy.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void DecodingAPayload_IsFasterThanParsingTheLevel()
        {
            var serialization = Serialization();
            var level = MockData.CreateLargeTestLevel(ObjectCount, PrefabCount, PrefabObjectCount);

            var json = serialization.SerializeEnvelope(level, SerializationType.Json);
            var payload = LevelCacheCodec.Write(serialization, level);

            // One of each first, so neither side is charged for its own JIT.
            serialization.DeserializeEnvelope<Level>(json, SerializationType.Json);
            LevelCacheCodec.TryRead(serialization, payload, out _);

            var parseWatch = Stopwatch.StartNew();
            serialization.DeserializeEnvelope<Level>(json, SerializationType.Json);
            parseWatch.Stop();

            var decodeWatch = Stopwatch.StartNew();
            Assert.IsTrue(LevelCacheCodec.TryRead(serialization, payload, out _));
            decodeWatch.Stop();

            TestContext.WriteLine($"parse {parseWatch.Elapsed.TotalMilliseconds:0.0} ms, " +
                                  $"decode {decodeWatch.Elapsed.TotalMilliseconds:0.0} ms, " +
                                  $"payload {payload.Length / 1024} KB against json {json.Length / 1024} KB");

            Assert.Less(decodeWatch.Elapsed.TotalMilliseconds, parseWatch.Elapsed.TotalMilliseconds / 2d,
                "Decoding a cache payload is no longer meaningfully faster than parsing the level - " +
                "the cache has stopped being worth its own complexity");
        }
    }
}
