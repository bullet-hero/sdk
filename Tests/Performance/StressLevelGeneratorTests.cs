using System;
using System.Collections.Generic;
using System.Linq;
using BH.SDK.Generators.Performance;
using BH.SDK.Models;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Rules;
using BH.SDK.Utils.Performance;
using BH.SDK.Validations;
using NUnit.Framework;

namespace BH.SDK.Tests.Performance
{
    /// <summary> Spec check 5, the SDK half: a stress level moves only the dimension it sweeps. </summary>
    public class StressLevelGeneratorTests
    {
        private const float Tolerance = 1e-3f;

        private static IEnumerable<DemandDimension> AllDimensions()
        {
            foreach (DemandDimension dimension in Enum.GetValues(typeof(DemandDimension))) yield return dimension;
        }

        [TestCaseSource(nameof(AllDimensions))]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Create_ThreeUnitCounts_MoveOnlyTheSweptDimension(DemandDimension dimension)
        {
            var baseline = StressBaseline.Of(64);
            var max = Math.Min(StressLevelGenerator.MaxUnits(dimension, baseline), 60);
            var counts = new[] { 0, max / 2, max };

            var measured = new List<float[]>();
            foreach (var units in counts)
            {
                var windows = LevelDemandCounter.Count(StressLevelGenerator.Create(dimension, units, baseline));
                Assert.AreEqual(3, windows.Count, "two window lengths make three windows");
                measured.Add(windows[1]);
            }

            for (var k = 0; k < DemandDimensions.Count; k++)
            {
                if (k == (int)dimension && !DemandDimensions.IsArea(dimension))
                {
                    for (var i = 1; i < counts.Length; i++)
                        Assert.AreEqual(counts[i] - counts[0], measured[i][k] - measured[0][k], Tolerance,
                            $"{dimension} reads exactly the units added");
                    continue;
                }

                for (var i = 1; i < counts.Length; i++)
                    Assert.AreEqual(measured[0][k], measured[i][k], Tolerance,
                        $"{(DemandDimension)k} moved while sweeping {dimension}");
            }
        }

        // The benchmark asks for counts no pool may reach (512 opaque batches); the search must still end.
        [TestCaseSource(nameof(AllDimensions))]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void PoolFor_AnUnreachableCount_EndsOnTheLargestPoolThatGrows(DemandDimension dimension)
        {
            var baseline = StressLevelGenerator.PoolFor(dimension, int.MaxValue);

            Assert.LessOrEqual(baseline.Pool, StressLevelGenerator.MaxPool, $"{dimension}");
            Assert.Greater(StressLevelGenerator.MaxUnits(dimension, baseline), 0, $"{dimension}");
        }

        [TestCaseSource(nameof(AllDimensions))]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void PoolFor_AReachableCount_HoldsIt(DemandDimension dimension)
        {
            var units = Math.Min(StressLevelGenerator.MaxUnits(dimension, StressBaseline.Of(64)), 100);
            var baseline = StressLevelGenerator.PoolFor(dimension, units);

            Assert.GreaterOrEqual(StressLevelGenerator.MaxUnits(dimension, baseline), units, $"{dimension}");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Create_APoolAboveTheLevelLimit_Throws()
        {
            var baseline = StressBaseline.Of(StressLevelGenerator.MaxPool + 1);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                StressLevelGenerator.Create(DemandDimension.Colliders, 0, baseline));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Create_MoreUnitsThanThePoolHolds_Throws()
        {
            var baseline = StressBaseline.Of(4);
            var max = StressLevelGenerator.MaxUnits(DemandDimension.Colliders, baseline);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                StressLevelGenerator.Create(DemandDimension.Colliders, max + 1, baseline));
        }

        [TestCaseSource(nameof(AllDimensions))]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Create_AtItsLargestUnitCount_IsAValidLevel(DemandDimension dimension)
        {
            var baseline = StressBaseline.Of(16);
            var units = Math.Min(StressLevelGenerator.MaxUnits(dimension, baseline), 4096);
            var level = StressLevelGenerator.Create(dimension, units, baseline);

            var errors = new RuleAnalyzer().Analyze(level, new RuleAnalyzerSettings())
                .Where(issue => issue.Rule.Group == RuleGroup.Error).ToList();

            Assert.IsEmpty(errors, $"{dimension}: " + string.Join("; ", errors.Select(issue => issue.ToString())));
        }

        private static IEnumerable<StressPart[]> Mixes()
        {
            yield return new[]
                { new StressPart(DemandDimension.TransparentArea, 4), new StressPart(DemandDimension.Effects, 16) };
            yield return new[]
                { new StressPart(DemandDimension.TransparentArea, 3), new StressPart(DemandDimension.PostPasses, 6) };
            yield return new[]
            {
                new StressPart(DemandDimension.Instances, 40), new StressPart(DemandDimension.Keyframes, 300),
                new StressPart(DemandDimension.ParentLinks, 90), new StressPart(DemandDimension.Glyphs, 70),
                new StressPart(DemandDimension.OpaqueBatches, 12), new StressPart(DemandDimension.Colliders, 20),
            };
        }

        // The additivity check predicts a mix from its parts; the level itself must be exactly that sum.
        [TestCaseSource(nameof(Mixes))]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void CreateMix_CountsAreTheSumOfItsParts(StressPart[] parts)
        {
            var mix = LevelDemandCounter.Count(StressLevelGenerator.CreateMix(parts))[1];
            var spine = LevelDemandCounter.Count(
                StressLevelGenerator.Create(DemandDimension.Instances, 0, StressBaseline.Of(0)))[1];

            var expected = new float[DemandDimensions.Count];
            foreach (var part in parts)
            {
                var baseline = StressLevelGenerator.PoolFor(part.Dimension, part.Units);
                var own = LevelDemandCounter.Count(StressLevelGenerator.Create(part.Dimension, part.Units,
                    baseline))[1];
                for (var k = 0; k < expected.Length; k++) expected[k] += own[k] - spine[k];
            }

            for (var k = 0; k < expected.Length; k++)
                Assert.AreEqual(expected[k] + spine[k], mix[k], Tolerance, $"{(DemandDimension)k}");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void CreateMix_ADimensionTwice_Throws()
        {
            var parts = new[]
                { new StressPart(DemandDimension.Effects, 2), new StressPart(DemandDimension.Effects, 3) };

            Assert.Throws<ArgumentException>(() => StressLevelGenerator.CreateMix(parts));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void CreateMix_MoreObjectsThanALevelHolds_Throws()
        {
            var instances = StressLevelGenerator.MaxUnits(DemandDimension.Instances, StressBaseline.Of(0));
            var parts = new[]
            {
                new StressPart(DemandDimension.Instances, instances), new StressPart(DemandDimension.Effects, 1),
            };

            Assert.Throws<ArgumentOutOfRangeException>(() => StressLevelGenerator.CreateMix(parts));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void CreateMix_IsAValidLevel()
        {
            var parts = Mixes().Last();
            var level = StressLevelGenerator.CreateMix(parts);

            var errors = new RuleAnalyzer().Analyze(level, new RuleAnalyzerSettings())
                .Where(issue => issue.Rule.Group == RuleGroup.Error).ToList();

            Assert.IsEmpty(errors, string.Join("; ", errors.Select(issue => issue.ToString())));
        }
    }
}