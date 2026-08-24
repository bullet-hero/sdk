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
    /// Splits color channels toward the screen edges, like a cheap lens. Cheaper than the glitch
    /// effects and often enough to sell an "impact" moment on its own.
    /// </summary>
    [RuleContainer]
    public class ChromaticAberrationKey : PostProcessingKeyframe, IModel<ChromaticAberrationKey>
    {
        /// <summary> How far the channels separate. </summary>
        [RuleInRange(PostProcessingRules.ChromaticAberration.IntensityMin,
            PostProcessingRules.ChromaticAberration.IntensityMax)]
        [JsonProperty(Names.Intensity)]
        public float Intensity { get; set; }

        public ChromaticAberrationKey()
        {
            Intensity = 1.0f;
        }
        public ChromaticAberrationKey(float intensity,
            bool active, int frame, EaseType ease = Keyframe.DefaultEase) : base(active, frame, ease)
        {
            Intensity = intensity;
        }
        public override void Reset()
        {
            base.Reset();
            Intensity = 1.0f;
        }
        
        public override object Clone() => CopyImpl();
        public override PostProcessingKeyframe Copy() => CopyImpl();
        ChromaticAberrationKey ICopyable<ChromaticAberrationKey>.Copy() => CopyImpl();
        
        private ChromaticAberrationKey CopyImpl() => new(Intensity, Active, Frame, Ease);

        public void Update(ChromaticAberrationKey src)
        {
            base.Update(src);

            Intensity = src.Intensity;
        }

        public void Pull(ChromaticAberrationKey src)
        {
            base.Pull(src);

            Intensity = src.Intensity;
        }

        public override bool Equals(object obj) => obj is ChromaticAberrationKey value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Intensity);

        public bool Equals(ChromaticAberrationKey other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other)
                         && Intensity.Equals(other.Intensity);
            return result;
        }
    }
}