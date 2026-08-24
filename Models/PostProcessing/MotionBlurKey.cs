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
    /// Smears the frame along camera motion. Quality and clamp are deliberately not authorable -
    /// quality is the player's setting and the clamp is fixed, so a level cannot make it unreadable.
    /// </summary>
    [RuleContainer]
    public class MotionBlurKey : PostProcessingKeyframe, IModel<MotionBlurKey> // HEAVY IN ANY CASE, PHONES DON'T LIKE IT
    {
        // Quality (client settings variable, he set it himself)

        /// <summary> Strength of the smear. </summary>
        [RuleInRange(PostProcessingRules.MotionBlur.IntensityMin,
            PostProcessingRules.MotionBlur.IntensityMax)]
        [JsonProperty(Names.Intensity)]
        public float Intensity { get; set; }
        
        // Clamp (0.2f, predefined)

        public MotionBlurKey()
        {
            Intensity = 1f;
        }
        public MotionBlurKey(float intensity, 
            bool active, int frame, EaseType ease = Keyframe.DefaultEase) : base(active, frame, ease)
        {
            Intensity = intensity;
        }
        public override void Reset()
        {
            base.Reset();
            Intensity = 1f;
        }

        public override object Clone() => CopyImpl();
        public override PostProcessingKeyframe Copy() => CopyImpl();
        MotionBlurKey ICopyable<MotionBlurKey>.Copy() => CopyImpl();
        
        private MotionBlurKey CopyImpl() => new(Intensity, Active, Frame, Ease);

        public void Update(MotionBlurKey src)
        {
            base.Update(src);

            Intensity = src.Intensity;
        }

        public void Pull(MotionBlurKey src)
        {
            base.Pull(src);

            Intensity = src.Intensity;
        }

        public override bool Equals(object obj) => obj is MotionBlurKey value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Intensity);

        public bool Equals(MotionBlurKey other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other)
                         && Intensity.Equals(other.Intensity);
            return result;
        }
    }
}