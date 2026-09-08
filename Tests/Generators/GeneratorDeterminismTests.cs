using System.Linq;
using BH.SDK.Generators;
using BH.SDK.Models;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Values;
using NUnit.Framework;

namespace BH.SDK.Tests.Generators
{
    // "Same seed, same level" is a promise this system makes to authors, and it is only worth
    // anything if it holds across runtimes - which is why GeneratorRandom is a hand-written
    // xorshift32 rather than System.Random. These tests pin both halves: the RNG's own sequence,
    // and the fact that a generator actually routes its randomness through it.

    /// <summary> "Same seed, same level", across runtimes: the hand-written xorshift's own sequence, and the
    /// fact that a generator actually routes its randomness through it. </summary>
    public class GeneratorDeterminismTests
    {
        private static Level CreateLevel()
        {
            var level = new Level();
            level.Settings.FrameDuration = 600;
            return level;
        }

        private static float[] RunScatter(uint seed)
        {
            var level = CreateLevel();
            var context = new GeneratorContext(level, FrameSpan.FromBounds(0, 60), seed: seed);
            new ScatterTestGenerator().Run(context, new ScatterTestGenerator.Parameters { Count = 16 });

            return level.Game.Objects.Values
                .Select(obj => ((Vector2Value)obj.Positions[0].Pos).X)
                .ToArray();
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void SameSeed_ProducesIdenticalOutput()
        {
            CollectionAssert.AreEqual(RunScatter(12345u), RunScatter(12345u));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void DifferentSeed_ProducesDifferentOutput()
        {
            CollectionAssert.AreNotEqual(RunScatter(1u), RunScatter(2u));
        }

        // Zero is remapped rather than rejected: xorshift32 is stuck at zero forever, and a default
        // GeneratorContext seed of 0 is the most likely one there is.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void ZeroSeed_StillProducesVaryingNumbers()
        {
            var random = new GeneratorRandom(0);
            var first = random.NextUInt();
            var second = random.NextUInt();

            Assert.AreNotEqual(0u, first);
            Assert.AreNotEqual(first, second);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void NextFloat_StaysInUnitRange()
        {
            var random = new GeneratorRandom(777u);
            for (var i = 0; i < 1000; i++)
            {
                var value = random.NextFloat();
                Assert.GreaterOrEqual(value, 0f);
                Assert.Less(value, 1f);
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void NextInt_StaysInRange_AndTreatsEmptyRangeAsMin()
        {
            var random = new GeneratorRandom(31u);
            for (var i = 0; i < 1000; i++)
            {
                var value = random.NextInt(-5, 5);
                Assert.GreaterOrEqual(value, -5);
                Assert.Less(value, 5);
            }

            Assert.AreEqual(3, random.NextInt(3, 3));
            Assert.AreEqual(3, random.NextInt(3, 1));
        }

        // Two instances on the same seed must not merely "look similar" - they must be the same
        // sequence, since that is what makes a generated level reproducible.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void TwoInstances_OnTheSameSeed_WalkTheSameSequence()
        {
            var a = new GeneratorRandom(42u);
            var b = new GeneratorRandom(42u);

            for (var i = 0; i < 100; i++)
                Assert.AreEqual(a.NextUInt(), b.NextUInt());
        }
    }
}
