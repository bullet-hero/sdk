using System;
using System.Collections.Generic;
using System.Linq;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Rules;
using BH.SDK.Utils.Performance;
using NUnit.Framework;

namespace BH.SDK.Tests.Performance
{
    /// <summary> Spec checks 1 and 2: the front loses no maximum of a monotone function, the merge only
    /// over-reads. </summary>
    public class ParetoFrontTests
    {
        private const int Dimensions = DemandDimensions.Count;
        private const float Tolerance = 1e-3f;

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Reduce_DropsADominatedWindow_AndKeepsEqualOnesOnce()
        {
            var windows = new List<float[]>
            {
                new[] { 1f, 1f }, new[] { 2f, 2f }, new[] { 2f, 2f }, new[] { 3f, 0f },
            };

            var front = ParetoFront.Reduce(windows, 10, out var exact, out var origins);

            Assert.IsTrue(exact);
            Assert.AreEqual(2, front.Count);
            CollectionAssert.AreEqual(new[] { 2f, 2f }, front[0]);
            CollectionAssert.AreEqual(new[] { 3f, 0f }, front[1]);
            CollectionAssert.AreEqual(new[] { 1, 3 }, origins, "the earliest of equal windows stands for them");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Reduce_AboveTheCap_MergesIntoTheComponentWiseMax()
        {
            var windows = new List<float[]> { new[] { 5f, 0f }, new[] { 4f, 1f }, new[] { 0f, 5f } };

            var front = ParetoFront.Reduce(windows, 2, out var exact, out var origins);

            Assert.IsFalse(exact);
            Assert.AreEqual(2, front.Count);
            CollectionAssert.AreEqual(new[] { 5f, 1f }, front[0], "the two closest became their max");
            CollectionAssert.AreEqual(new[] { 0, 2 }, origins);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Reduce_DoesNotModifyItsInput()
        {
            var first = new[] { 5f, 0f };
            var windows = new List<float[]> { first, new[] { 4f, 1f }, new[] { 0f, 5f } };

            ParetoFront.Reduce(windows, 1, out _);

            CollectionAssert.AreEqual(new[] { 5f, 0f }, first);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Reduce_RandomSets_KeepEveryMaximumOfALinearCost()
        {
            var random = new Random(1234);
            var merged = 0;

            for (var set = 0; set < 1000; set++)
            {
                var windows = RandomWindows(random);
                var front = ParetoFront.Reduce(windows, PerformanceRules.MaxDemandWindows, out var exact);
                if (!exact) merged++;

                Assert.LessOrEqual(front.Count, PerformanceRules.MaxDemandWindows);

                for (var trial = 0; trial < 100; trial++)
                {
                    var c1 = RandomCost(random);
                    var c2 = RandomCost(random);

                    AssertMax(windows, front, exact, w => Dot(c1, w));
                    AssertMax(windows, front, exact, w => Math.Max(Dot(c1, w), Dot(c2, w)));
                }
            }

            Assert.Greater(merged, 0, "the random sets must exercise the merge too");
            Assert.Less(merged, 1000, "and the exact path");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Reduce_IsDeterministic()
        {
            var windows = RandomWindows(new Random(7), 400);

            var first = ParetoFront.Reduce(windows, PerformanceRules.MaxDemandWindows, out _, out var a);
            var second = ParetoFront.Reduce(windows, PerformanceRules.MaxDemandWindows, out _, out var b);

            CollectionAssert.AreEqual(a, b);
            for (var i = 0; i < first.Count; i++) CollectionAssert.AreEqual(first[i], second[i]);
        }

        private static void AssertMax(List<float[]> all, List<float[]> front, bool exact, Func<float[], double> cost)
        {
            var full = all.Max(cost);
            var reduced = front.Max(cost);

            if (exact) Assert.AreEqual(full, reduced, Tolerance * Math.Max(1d, full));
            else Assert.GreaterOrEqual(reduced, full - Tolerance * Math.Max(1d, full));
        }

        // Correlated content (a shared intensity) plus noise: some sets keep a small front, some do not.
        private static List<float[]> RandomWindows(Random random, int count = 0)
        {
            if (count == 0) count = random.Next(1, 120);
            var noise = random.NextDouble();
            var windows = new List<float[]>(count);

            for (var i = 0; i < count; i++)
            {
                var intensity = random.NextDouble() * 100d;
                var window = new float[Dimensions];
                for (var k = 0; k < Dimensions; k++)
                    window[k] = (float)(intensity * (1d + noise * (random.NextDouble() - 0.5d)));
                windows.Add(window);
            }

            return windows;
        }

        private static float[] RandomCost(Random random)
        {
            var cost = new float[Dimensions];
            for (var k = 0; k < Dimensions; k++) cost[k] = (float)random.NextDouble();
            return cost;
        }

        private static double Dot(float[] c, float[] x)
        {
            var sum = 0d;
            for (var k = 0; k < c.Length; k++) sum += (double)c[k] * x[k];
            return sum;
        }
    }
}
