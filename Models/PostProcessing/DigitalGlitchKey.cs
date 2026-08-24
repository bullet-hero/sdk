using System;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Keyframes;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.PostProcessing
{
    /// <summary>
    /// Corrupted-datastream artifacts: displaced blocks and color garbage. One dial, unlike the
    /// four-way AnalogGlitchKey, because block corruption has no separate axes to tune.
    /// </summary>
    [RuleContainer]
    public class DigitalGlitchKey : PostProcessingKeyframe, IModel<DigitalGlitchKey> // HEAVY IN ANY CASE, PHONES DON'T LIKE IT
    {
        /// <summary> How much of the frame gets corrupted. </summary>
        [RuleInRange(PostProcessingRules.DigitalGlitch.IntensityMin,
            PostProcessingRules.DigitalGlitch.IntensityMax)]
        [JsonProperty(Names.Intensity)]
        public float Intensity { get; set; }

        public DigitalGlitchKey()
        {
            Intensity = 0.1f;
        }
        public DigitalGlitchKey(float intensity,
            bool active, int frame, EaseType ease = Keyframe.DefaultEase) : base(active, frame, ease)
        {
            Intensity = intensity;
        }
        public override void Reset()
        {
            base.Reset();
            Intensity = 0.1f;
        }
        
        public override object Clone() => CopyImpl();
        public override PostProcessingKeyframe Copy() => CopyImpl();
        DigitalGlitchKey ICopyable<DigitalGlitchKey>.Copy() => CopyImpl();
        
        private DigitalGlitchKey CopyImpl() => new(Intensity, Active, Frame, Ease);

        public void Update(DigitalGlitchKey src)
        {
            base.Update(src);

            Intensity = src.Intensity;
        }

        public void Pull(DigitalGlitchKey src)
        {
            base.Pull(src);

            Intensity = src.Intensity;
        }

        public override bool Equals(object obj) => obj is DigitalGlitchKey value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Intensity);

        public bool Equals(DigitalGlitchKey other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other)
                         && Intensity.Equals(other.Intensity);
            return result;
        }
    }
}