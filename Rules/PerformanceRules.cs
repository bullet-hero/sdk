using BH.SDK.Models.Enums.Settings;

namespace BH.SDK.Rules
{
    // Starting points the validation checks may move (Docs/Plans/LEVEL_PERFORMANCE_RATING_SPEC.md).

    /// <summary> The constants of the level performance rating: the window grid a level's demand is
    /// measured on, the bands a score is read in, and the gates the device stress test must pass. </summary>
    public static class PerformanceRules
    {
        /// <summary> Length of one demand window, in seconds. </summary>
        public const float WindowSeconds = 0.5f;

        /// <summary> How far the next window starts after the previous one, in seconds. </summary>
        public const float WindowHopSeconds = 0.25f;

        /// <summary> How many frames per second of level time the client samples for the area
        /// dimensions. </summary>
        public const int AreaSamplesPerSecond = 8;

        /// <summary> The most windows a stored demand keeps; above it windows are merged conservatively.
        /// </summary>
        public const int MaxDemandWindows = 32;

        /// <summary> What a demand's dimensions mean. Raised when one changes; a demand carrying another
        /// value reads as unknown. </summary>
        public const int AnalyzerVersion = 1;

        /// <summary> What a device profile's coefficients mean. Raised when the stress test changes; a
        /// profile carrying another value is ignored. </summary>
        public const int BenchmarkVersion = 1;

        /// <summary> Below this score a level holds on the device. </summary>
        public const float ScoreWarning = 70f;

        /// <summary> Above this score a level does not hold: exactly the frame budget. </summary>
        public const float ScoreLimit = 100f;

        /// <summary> The most points one stress test sweep measures. </summary>
        public const int BenchmarkMaxPoints = 6;

        /// <summary> The least R² a sweep's linear fit may have before the profile is untrusted. </summary>
        public const float MinFitR2 = 0.98f;

        /// <summary> The largest relative difference between the up and the down slope of the thermal
        /// check. </summary>
        public const float MaxDriftError = 0.10f;

        /// <summary> The largest relative error of a mixed stress level's predicted frame time. </summary>
        public const float MaxAdditivityError = 0.15f;

        /// <summary> The least Spearman correlation of predicted against measured corpus windows. </summary>
        public const float MinRankCorrelation = 0.8f;

        /// <summary> The largest relative error of a corpus level's predicted peak. </summary>
        public const float MaxPeakError = 0.25f;

        /// <summary> How many anti-aliasing modes a device profile carries an area factor for: none, MSAA
        /// x2, x4, x8, FXAA. </summary>
        public const int AreaFactorCount = 5;

        /// <summary> Which <see cref="Models.Device.DeviceProfile.AreaFactors"/> slot a mode reads.
        /// </summary>
        public static int AreaFactorIndex(AntiAliasingType type, MsaaType msaa) => type switch
        {
            AntiAliasingType.Msaa => msaa switch
            {
                MsaaType.X2 => 1,
                MsaaType.X4 => 2,
                MsaaType.X8 => 3,
                _ => 0,
            },
            AntiAliasingType.Fxaa => 4,
            _ => 0,
        };

        /// <summary> Area factors that change nothing, one per mode. </summary>
        public static float[] NeutralAreaFactors()
        {
            var factors = new float[AreaFactorCount];
            for (var i = 0; i < factors.Length; i++) factors[i] = 1f;
            return factors;
        }
    }
}