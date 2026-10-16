using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Versions;
using Newtonsoft.Json;

namespace BH.SDK.Models.Device
{
    // A new domain in its own file, so no bump. Bound to the device it was measured on, which is why profile
    // transfer leaves it behind. Docs/PERFORMANCE_RATING.md.

    /// <summary> What a device can do: milliseconds per unit of every <see cref="DemandDimension"/>, CPU and
    /// GPU apart, as the stress test fitted them. </summary>
    [RuleContainer]
    [ModelGeneration(ModelDomains.DeviceProfile, ModelGenerations.V3_ImproveAppearance)]
    [GenerateModel]
    public sealed partial class DeviceProfile : IModel<DeviceProfile>
    {
        /// <summary> The <see cref="PerformanceRules.BenchmarkVersion"/> that measured this; any other value
        /// is ignored. </summary>
        [JsonProperty(Names.BenchmarkVersion)]
        public int BenchmarkVersion { get; set; }

        /// <summary> The device model the profile was measured on. </summary>
        [RuleNotNull]
        [JsonProperty(Names.DeviceModel)]
        public string DeviceModel { get; set; }

        /// <summary> The graphics device the profile was measured on. </summary>
        [RuleNotNull]
        [JsonProperty(Names.GpuName)]
        public string GpuName { get; set; }

        /// <summary> Native screen width in pixels at measurement. </summary>
        [RuleMinValue(0)]
        [JsonProperty(Names.ScreenWidth)]
        public int ScreenWidth { get; set; }

        /// <summary> Native screen height in pixels at measurement. </summary>
        [RuleMinValue(0)]
        [JsonProperty(Names.ScreenHeight)]
        public int ScreenHeight { get; set; }

        /// <summary> Whether GPU time was measured. False: every cost was fitted from the frame interval into
        /// the CPU side, and the GPU side is zero. </summary>
        [JsonProperty(Names.GpuTiming)]
        public bool GpuTiming { get; set; }

        /// <summary> CPU milliseconds of a frame with nothing in it. </summary>
        [RuleMinValue(0f)]
        [JsonProperty(Names.CpuBase)]
        public float CpuBase { get; set; }

        /// <summary> GPU milliseconds of a frame with nothing in it. </summary>
        [RuleMinValue(0f)]
        [JsonProperty(Names.GpuBase)]
        public float GpuBase { get; set; }

        /// <summary> CPU milliseconds per unit, one per <see cref="DemandDimension"/>. </summary>
        [RuleNotNull]
        [JsonProperty(Names.CpuCoefficients)]
        public float[] CpuCoefficients { get; set; }

        /// <summary> GPU milliseconds per unit, one per <see cref="DemandDimension"/>. </summary>
        [RuleNotNull]
        [JsonProperty(Names.GpuCoefficients)]
        public float[] GpuCoefficients { get; set; }

        /// <summary> R² of each CPU fit, one per <see cref="DemandDimension"/>. </summary>
        [RuleNotNull]
        [JsonProperty(Names.CpuFit)]
        public float[] CpuFit { get; set; }

        /// <summary> R² of each GPU fit, one per <see cref="DemandDimension"/>. </summary>
        [RuleNotNull]
        [JsonProperty(Names.GpuFit)]
        public float[] GpuFit { get; set; }

        /// <summary> How much each anti-aliasing mode multiplies the area cost, indexed by
        /// <see cref="PerformanceRules.AreaFactorIndex"/>; 1 is no change. </summary>
        [RuleNotNull]
        [JsonProperty(Names.AreaFactors)]
        public float[] AreaFactors { get; set; }

        /// <summary> Relative difference of the up and down slopes of the thermal check. </summary>
        [RuleMinValue(0f)]
        [JsonProperty(Names.DriftError)]
        public float DriftError { get; set; }

        /// <summary> Whether every fit and the drift check passed their gates; an untrusted profile is
        /// never used. </summary>
        [JsonProperty(Names.Trusted)]
        public bool Trusted { get; set; }

        /// <summary> When the stress test ran, UTC. </summary>
        [JsonProperty(Names.MeasuredAt)]
        public DateTime MeasuredAt { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public DeviceProfile()
        {
            BenchmarkVersion = PerformanceRules.BenchmarkVersion;
            DeviceModel = string.Empty;
            GpuName = string.Empty;
            ScreenWidth = 0;
            ScreenHeight = 0;
            GpuTiming = true;
            CpuBase = 0f;
            GpuBase = 0f;
            CpuCoefficients = new float[DemandDimensions.Count];
            GpuCoefficients = new float[DemandDimensions.Count];
            CpuFit = new float[DemandDimensions.Count];
            GpuFit = new float[DemandDimensions.Count];
            AreaFactors = PerformanceRules.NeutralAreaFactors();
            DriftError = 0f;
            Trusted = false;
            MeasuredAt = default;
        }

        // Not Trusted: an untrusted profile is still this device's own measurement, closer than any
        // reference; the flag only tells the player that measuring again may be more exact.

        /// <summary> Whether this build can score with it: measured the way it measures. </summary>
        public static bool IsUsable(DeviceProfile profile)
            => profile != null && profile.BenchmarkVersion == PerformanceRules.BenchmarkVersion
               && profile.CpuCoefficients != null && profile.GpuCoefficients != null;
    }
}
