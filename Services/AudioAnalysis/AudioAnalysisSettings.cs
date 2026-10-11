using System;

namespace BH.SDK.Services.AudioAnalysis
{
    /// <summary> What the caller may choose about one analysis; everything else is
    /// <see cref="AudioAnalysisRules"/>. </summary>
    public sealed class AudioAnalysisSettings
    {
        /// <summary> Beats in a bar: which candidates the downbeat search weighs, and the section windows.
        /// </summary>
        public int BeatsPerBar { get; set; } = AudioAnalysisRules.DefaultBeatsPerBar;

        /// <summary> Slowest tempo tried. </summary>
        public float MinBpm { get; set; } = AudioAnalysisRules.MinBpm;

        /// <summary> Fastest tempo tried. </summary>
        public float MaxBpm { get; set; } = AudioAnalysisRules.MaxBpm;

        /// <summary> A fresh instance at the defaults. </summary>
        public static AudioAnalysisSettings Default => new();

        /// <summary> Throws when a value cannot be analysed with. </summary>
        /// <exception cref="ArgumentException"> The message names the setting. </exception>
        public void Validate()
        {
            if (BeatsPerBar < 1) throw new ArgumentException($"BeatsPerBar must be at least 1, got {BeatsPerBar}.");
            if (!(MinBpm > 0f) || float.IsInfinity(MinBpm))
                throw new ArgumentException($"MinBpm must be positive, got {MinBpm}.");
            if (!(MaxBpm >= MinBpm) || float.IsInfinity(MaxBpm))
                throw new ArgumentException($"MaxBpm must be at least MinBpm ({MinBpm}), got {MaxBpm}.");
        }
    }
}
