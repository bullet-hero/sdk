using System;
using System.Collections.Generic;
using BH.SDK.Models.Device;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Meta;
using BH.SDK.Rules;
using BH.SDK.Utils.Performance;
using NUnit.Framework;

namespace BH.SDK.Tests.Performance
{
    /// <summary> The score formula: unknown inputs, the bands, spec checks 3 (monotone) and 4 (render
    /// scale is exactly r²). </summary>
    public class PerformanceScoreTests
    {
        private const float Tolerance = 1e-3f;

        private static DeviceProfile Profile(float cpuBase = 2f, float gpuBase = 3f, float coefficient = 0.01f)
        {
            var profile = new DeviceProfile { CpuBase = cpuBase, GpuBase = gpuBase, Trusted = true };
            for (var k = 0; k < DemandDimensions.Count; k++)
            {
                profile.CpuCoefficients[k] = coefficient;
                profile.GpuCoefficients[k] = coefficient * 2f;
            }

            return profile;
        }

        private static LevelDemand Demand(params float[][] windows)
        {
            var demand = new LevelDemand();
            for (var i = 0; i < windows.Length; i++) demand.Windows.Add(new DemandWindow(i * 0.25f, windows[i]));
            return demand;
        }

        private static float[] Window(float value)
        {
            var window = new float[DemandDimensions.Count];
            for (var k = 0; k < window.Length; k++) window[k] = value;
            return window;
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Evaluate_NullOrStaleDemand_IsUnknown_NeverZero()
        {
            var stale = Demand(Window(1f));
            stale.AnalyzerVersion = PerformanceRules.AnalyzerVersion + 1;

            var none = PerformanceScore.Evaluate(null, Profile(), ScoreSettings.Default);
            var old = PerformanceScore.Evaluate(stale, Profile(), ScoreSettings.Default);

            Assert.IsFalse(none.Known);
            Assert.IsFalse(old.Known);
            Assert.AreEqual(PerformanceBand.Unknown, none.Band);
            Assert.AreEqual(-1, old.HottestWindow);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Evaluate_AnAllZeroDemand_ScoresTheLargerBaseAgainstTheBudget()
        {
            var settings = ScoreSettings.Default;
            var result = PerformanceScore.Evaluate(Demand(Window(0f)), Profile(2f, 3f), settings);

            Assert.IsTrue(result.Known);
            Assert.AreEqual(100f * 3f / (1000f / settings.TargetFps), result.Score, Tolerance);
            Assert.IsTrue(result.GpuBound);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Evaluate_NamesThePeakWindow()
        {
            var result = PerformanceScore.Evaluate(Demand(Window(1f), Window(50f), Window(3f)), Profile(),
                ScoreSettings.Default);

            Assert.AreEqual(1, result.HottestWindow);
        }

        [TestCase(10f, PerformanceBand.Holds)]
        [TestCase(PerformanceRules.ScoreWarning, PerformanceBand.Tight)]
        [TestCase(PerformanceRules.ScoreLimit, PerformanceBand.Tight)]
        [TestCase(PerformanceRules.ScoreLimit + 1f, PerformanceBand.Drops)]
        [TestCase(PerformanceRules.ScoreHeavy, PerformanceBand.Drops)]
        [TestCase(PerformanceRules.ScoreHeavy + 1f, PerformanceBand.SevereDrops)]
        [TestCase(PerformanceRules.ScoreSevere, PerformanceBand.SevereDrops)]
        [TestCase(PerformanceRules.ScoreSevere + 1f, PerformanceBand.DoesNotHold)]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Band_ReadsTheScoreAgainstTheFourThresholds(float score, PerformanceBand band)
            => Assert.AreEqual(band, new ScoreResult(true, score, false, 0).Band);

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Evaluate_RenderScale_MultipliesThePixelTermsByItsSquare()
        {
            var profile = Profile(0f, 0f, 0f);
            profile.GpuCoefficients[(int)DemandDimension.TransparentArea] = 2f;
            profile.GpuCoefficients[(int)DemandDimension.OpaqueArea] = 1f;
            profile.GpuCoefficients[(int)DemandDimension.PostPasses] = 0.5f;
            var demand = Demand(Window(1f));

            var native = ScoreSettings.Default;
            var scaled = native;
            scaled.RenderScale = 0.7f;

            var full = PerformanceScore.Evaluate(demand, profile, native).Score;
            var reduced = PerformanceScore.Evaluate(demand, profile, scaled).Score;

            Assert.AreEqual(full * 0.49f, reduced, Tolerance);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Evaluate_EffectsOffAndPostOff_DropTheirTerms_AndFxaaBringsOnePassBack()
        {
            var profile = Profile(0f, 0f, 0f);
            profile.GpuCoefficients[(int)DemandDimension.Effects] = 1f;
            profile.GpuCoefficients[(int)DemandDimension.PostPasses] = 1f;
            var demand = Demand(Window(3f));

            var off = ScoreSettings.Default;
            off.Effects = false;
            off.PostProcessing = false;
            Assert.AreEqual(0f, PerformanceScore.Evaluate(demand, profile, off).Score, Tolerance);

            off.AntiAliasing = AntiAliasingType.Fxaa;
            var budget = 1000f / off.TargetFps;
            Assert.AreEqual(100f * 1f / budget, PerformanceScore.Evaluate(demand, profile, off).Score, Tolerance);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Evaluate_AFixedEffectRate_ScalesTheEffectTermByItsShareOfTheTarget()
        {
            var profile = Profile(0f, 0f, 0f);
            profile.CpuCoefficients[(int)DemandDimension.Effects] = 1f;
            var demand = Demand(Window(4f));

            var every = ScoreSettings.Default;
            var half = every;
            half.EffectsFps = every.TargetFps / 2;

            Assert.AreEqual(PerformanceScore.Evaluate(demand, profile, every).Score / 2f,
                PerformanceScore.Evaluate(demand, profile, half).Score, Tolerance);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Evaluate_IsMonotoneInEveryDemandValueAndEveryCoefficient()
        {
            var random = new Random(99);
            var settings = ScoreSettings.Default;
            settings.AntiAliasing = AntiAliasingType.Msaa;

            for (var trial = 0; trial < 500; trial++)
            {
                var profile = RandomProfile(random);
                var demand = Demand(RandomWindow(random), RandomWindow(random), RandomWindow(random));
                var score = PerformanceScore.Evaluate(demand, profile, settings).Score;

                var k = random.Next(DemandDimensions.Count);
                var w = random.Next(demand.Windows.Count);
                demand.Windows[w].Values[k] += (float)random.NextDouble() * 10f;
                var raised = PerformanceScore.Evaluate(demand, profile, settings).Score;
                Assert.GreaterOrEqual(raised, score - Tolerance, $"raising x[{(DemandDimension)k}]");

                var costs = random.Next(2) == 0 ? profile.CpuCoefficients : profile.GpuCoefficients;
                costs[k] += (float)random.NextDouble();
                Assert.GreaterOrEqual(PerformanceScore.Evaluate(demand, profile, settings).Score, raised - Tolerance,
                    $"raising c[{(DemandDimension)k}]");
            }
        }

        private static DeviceProfile RandomProfile(Random random)
        {
            var profile = Profile((float)random.NextDouble() * 5f, (float)random.NextDouble() * 5f, 0f);
            for (var k = 0; k < DemandDimensions.Count; k++)
            {
                profile.CpuCoefficients[k] = (float)random.NextDouble() * 0.1f;
                profile.GpuCoefficients[k] = (float)random.NextDouble() * 0.1f;
            }

            for (var i = 0; i < profile.AreaFactors.Length; i++) profile.AreaFactors[i] = 1f + (float)random.NextDouble();
            return profile;
        }

        private static float[] RandomWindow(Random random)
        {
            var window = new float[DemandDimensions.Count];
            for (var k = 0; k < window.Length; k++) window[k] = (float)random.NextDouble() * 100f;
            return window;
        }
    }
}
