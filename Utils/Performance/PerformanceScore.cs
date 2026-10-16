using System;
using BH.SDK.Models.Device;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Models.Enums.Settings;
using BH.SDK.Models.Meta;
using BH.SDK.Rules;

namespace BH.SDK.Utils.Performance
{
    /// <summary> The graphics settings a score is computed for, already resolved to numbers. </summary>
    public struct ScoreSettings
    {
        /// <summary> Frames per second the device aims at; the budget is <c>1000 / TargetFps</c> ms.
        /// </summary>
        public int TargetFps;

        /// <summary> Render scale, 1 being native. </summary>
        public float RenderScale;

        /// <summary> Anti-aliasing method. </summary>
        public AntiAliasingType AntiAliasing;

        /// <summary> MSAA sample count, read when <see cref="AntiAliasing"/> is MSAA. </summary>
        public MsaaType Msaa;

        /// <summary> Whether effects render at all. </summary>
        public bool Effects;

        /// <summary> The fixed effect update rate, or zero when effects update every frame. </summary>
        public int EffectsFps;

        /// <summary> Whether post-processing renders at all. </summary>
        public bool PostProcessing;

        /// <summary> Native settings at 60 fps, every switch on. </summary>
        public static ScoreSettings Default => new()
        {
            TargetFps = 60,
            RenderScale = 1f,
            AntiAliasing = AntiAliasingType.None,
            Msaa = MsaaType.X2,
            Effects = true,
            EffectsFps = 0,
            PostProcessing = true,
        };
    }

    /// <summary> How a level scores on a device: unknown, or the peak frame time against the budget.
    /// </summary>
    public readonly struct ScoreResult
    {
        /// <summary> False when the demand or the profile cannot be read by this build. </summary>
        public readonly bool Known;

        /// <summary> 100 is exactly the frame budget at the target frame rate. </summary>
        public readonly float Score;

        /// <summary> Whether the GPU side decided the peak. </summary>
        public readonly bool GpuBound;

        /// <summary> Index into <see cref="LevelDemand.Windows"/> of the peak window; -1 when unknown.
        /// </summary>
        public readonly int HottestWindow;

        /// <summary> Every member at once, in declaration order. </summary>
        public ScoreResult(bool known, float score, bool gpuBound, int hottestWindow)
        {
            Known = known;
            Score = score;
            GpuBound = gpuBound;
            HottestWindow = hottestWindow;
        }

        /// <summary> The result of a demand or profile this build cannot score. </summary>
        public static ScoreResult Unknown => new(false, 0f, false, -1);

        /// <summary> Which band the score falls in. </summary>
        public PerformanceBand Band => !Known ? PerformanceBand.Unknown
            : Score < PerformanceRules.ScoreWarning ? PerformanceBand.Holds
            : Score <= PerformanceRules.ScoreLimit ? PerformanceBand.Tight
            : Score <= PerformanceRules.ScoreHeavy ? PerformanceBand.Drops
            : Score <= PerformanceRules.ScoreSevere ? PerformanceBand.SevereDrops
            : PerformanceBand.DoesNotHold;
    }

    // Never stored: a band is read from a score each time, so its values may be renumbered freely.

    /// <summary> The five readings of a score, lightest first, plus not knowing. </summary>
    public enum PerformanceBand : byte
    {
        /// <summary> No current demand or no usable profile. </summary>
        Unknown = 0,

        /// <summary> Below <see cref="PerformanceRules.ScoreWarning"/>. </summary>
        Holds = 1,

        /// <summary> Up to <see cref="PerformanceRules.ScoreLimit"/>. </summary>
        Tight = 2,

        /// <summary> Up to <see cref="PerformanceRules.ScoreHeavy"/>. </summary>
        Drops = 3,

        /// <summary> Up to <see cref="PerformanceRules.ScoreSevere"/>. </summary>
        SevereDrops = 4,

        /// <summary> Above <see cref="PerformanceRules.ScoreSevere"/>. </summary>
        DoesNotHold = 5,
    }

    /// <summary> What a band means beside its name. </summary>
    public static class PerformanceBands
    {
        /// <summary> The bands a level holds in: up to <see cref="PerformanceRules.ScoreLimit"/>. </summary>
        public static bool Holds(PerformanceBand band) => band is PerformanceBand.Holds or PerformanceBand.Tight;

        /// <summary> The bands a level does not hold in: above <see cref="PerformanceRules.ScoreLimit"/>.
        /// </summary>
        public static bool Fails(PerformanceBand band) => band is PerformanceBand.Drops or PerformanceBand.SevereDrops
            or PerformanceBand.DoesNotHold;
    }

    // The formula and the settings multipliers: Docs/PERFORMANCE_RATING.md, "The score".

    /// <summary> Combines a level's demand with a device's profile under a set of graphics settings. Pure,
    /// monotone in every demand value and every coefficient. </summary>
    public static class PerformanceScore
    {
        /// <summary> The score of the peak window; unknown when either input cannot be read. </summary>
        public static ScoreResult Evaluate(LevelDemand demand, DeviceProfile profile, ScoreSettings settings)
        {
            if (!LevelDemand.IsCurrent(demand) || profile?.CpuCoefficients == null
                                               || profile.GpuCoefficients == null || settings.TargetFps <= 0)
                return ScoreResult.Unknown;

            var multipliers = Multipliers(profile, settings, out var extraPasses);
            var budget = 1000f / settings.TargetFps;

            var peak = Math.Max(profile.CpuBase, profile.GpuBase);
            var gpuBound = profile.GpuBase > profile.CpuBase;
            var hottest = -1;

            for (var w = 0; w < demand.Windows.Count; w++)
            {
                var window = demand.Windows[w];
                if (window == null) continue;

                var cpu = Time(profile.CpuBase, profile.CpuCoefficients, window, multipliers, extraPasses);
                var gpu = Time(profile.GpuBase, profile.GpuCoefficients, window, multipliers, extraPasses);
                var time = Math.Max(cpu, gpu);
                if (hottest >= 0 && time <= peak) continue;

                peak = time;
                gpuBound = gpu > cpu;
                hottest = w;
            }

            return new ScoreResult(true, 100f * peak / budget, gpuBound, hottest);
        }

        /// <summary> The frame time one window predicts, the larger of the CPU and GPU sides. </summary>
        public static float WindowTime(DemandWindow window, DeviceProfile profile, ScoreSettings settings)
        {
            var (cpu, gpu) = WindowSides(window, profile, settings);
            return Math.Max(cpu, gpu);
        }

        /// <summary> Both sides of the frame time one window predicts, ms. </summary>
        public static (float Cpu, float Gpu) WindowSides(DemandWindow window, DeviceProfile profile,
            ScoreSettings settings)
        {
            var multipliers = Multipliers(profile, settings, out var extraPasses);
            return (Time(profile.CpuBase, profile.CpuCoefficients, window, multipliers, extraPasses),
                Time(profile.GpuBase, profile.GpuCoefficients, window, multipliers, extraPasses));
        }

        /// <summary> What each dimension is multiplied by under these settings; <paramref name="extraPasses"/>
        /// is the pass FXAA brings back when post-processing is off. </summary>
        public static float[] Multipliers(DeviceProfile profile, ScoreSettings settings, out float extraPasses)
        {
            var multipliers = new float[DemandDimensions.Count];
            for (var k = 0; k < multipliers.Length; k++) multipliers[k] = 1f;

            var scale = Math.Max(0f, settings.RenderScale);
            var pixels = scale * scale;

            var factors = profile?.AreaFactors;
            var index = PerformanceRules.AreaFactorIndex(settings.AntiAliasing, settings.Msaa);
            var antiAliasing = factors != null && index < factors.Length ? Math.Max(0f, factors[index]) : 1f;

            multipliers[(int)DemandDimension.TransparentArea] = pixels * antiAliasing;
            multipliers[(int)DemandDimension.OpaqueArea] = pixels * antiAliasing;

            multipliers[(int)DemandDimension.Effects] = !settings.Effects
                ? 0f
                : settings.EffectsFps > 0 && settings.TargetFps > 0
                    ? Math.Min(1f, (float)settings.EffectsFps / settings.TargetFps)
                    : 1f;

            multipliers[(int)DemandDimension.PostPasses] = settings.PostProcessing ? pixels : 0f;
            extraPasses = !settings.PostProcessing && settings.AntiAliasing == AntiAliasingType.Fxaa ? pixels : 0f;
            return multipliers;
        }

        private static float Time(float baseline, float[] coefficients, DemandWindow window, float[] multipliers,
            float extraPasses)
        {
            var time = Math.Max(0f, baseline);
            var values = window.Values;
            if (values == null) return time;

            var count = Math.Min(values.Length, Math.Min(coefficients.Length, multipliers.Length));
            for (var k = 0; k < count; k++)
                time += Math.Max(0f, coefficients[k]) * multipliers[k] * Math.Max(0f, values[k]);

            var passes = (int)DemandDimension.PostPasses;
            if (extraPasses > 0f && passes < coefficients.Length)
                time += Math.Max(0f, coefficients[passes]) * extraPasses;

            return time;
        }
    }
}