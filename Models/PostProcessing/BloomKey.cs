using System;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.PostProcessing
{
    /// <summary>
    /// Glow bleeding out of bright pixels. Threshold is pinned at 0, so everything glows in
    /// proportion to its brightness rather than only what crosses a cutoff.
    /// </summary>
    [RuleContainer]
    public class BloomKey : PostProcessingKeyframe, IModel<BloomKey> // HEAVY IN ANY CASE, PHONES DON'T LIKE IT
    {
        // Threshold - 0 (always, not a parameter)

        /// <summary> Strength of the glow. </summary>
        [RuleInRange(PostProcessingRules.Bloom.IntensityMin,
           PostProcessingRules.Bloom.IntensityMax)]
        [JsonProperty(Names.Intensity)]
        public float Intensity { get; set; }

        /// <summary> How far the glow spreads from its source. </summary>
        [RuleInRange(PostProcessingRules.Bloom.ScatterMin,
            PostProcessingRules.Bloom.ScatterMax)]
        [JsonProperty(Names.Scatter)]
        public float Scatter { get; set; }

        /// <summary> Tint of the glow, independent of the source pixel's own color. </summary>
        [RuleNotNull(typeof(Color4Value))]
        [JsonProperty(Names.Color)]
        public IColor4 Color4 { get; set; }
        
        // Filter (player choose in settings: high - Gaussian, mid - Dual, low - Kawase)

        public BloomKey()
        {
            Intensity = 0.5f;
            Scatter = 0.5f;
            Color4 = Color4Value.red;
        }
        public BloomKey(float intensity, float scatter, IColor4 color4,
            bool active, int frame, EaseType ease = Keyframe.DefaultEase) : base(active, frame, ease)
        {
            Intensity = intensity;
            Scatter = scatter;
            Color4 = color4;
        }
        public override void Reset()
        {
            base.Reset();
            Intensity = 0.5f;
            Scatter = 0.5f;
            Color4 = Color4Value.red;
        }
        
        public override object Clone() => CopyImpl();
        public override PostProcessingKeyframe Copy() => CopyImpl();
        BloomKey ICopyable<BloomKey>.Copy() => CopyImpl();
        
        private BloomKey CopyImpl() => new(Intensity, Scatter, Color4.Copy(), Active, Frame, Ease);

        public void Update(BloomKey src)
        {
            base.Update(src);

            Intensity = src.Intensity;
            Scatter = src.Scatter;
            Color4 = src.Color4.Copy();
        }

        public void Pull(BloomKey src)
        {
            base.Pull(src);

            Intensity = src.Intensity;
            Scatter = src.Scatter;
            Color4 = Color4.PullFrom(src.Color4);
        }

        public override bool Equals(object obj) => obj is BloomKey value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Intensity, Scatter, Color4);

        public bool Equals(BloomKey other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other)
                         && Intensity.Equals(other.Intensity)
                         && Scatter.Equals(other.Scatter)
                         && Color4.Equals(other.Color4);
            return result;
        }
    }
}