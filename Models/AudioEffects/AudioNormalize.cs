using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.AudioEffects
{
    /// <summary>
    /// Continuously brings the signal toward a target loudness - what makes clips from different
    /// sources sit at a comparable level without hand-tuning each one.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class AudioNormalize : AudioEffect, IModel<AudioNormalize>
    {
        /// <summary> Seconds the gain takes to settle, so the correction is not audible as a jump. </summary>
        [RuleInRange(AudioRules.Normalize.FadeInTime_Min, AudioRules.Normalize.FadeInTime_Max)]
        [JsonProperty(Names.FadeInTime)]
        public float FadeInTime { get; set; }

        /// <summary> Signals quieter than this are left alone - keeps silence and noise floors from
        /// being amplified into hiss. </summary>
        [RuleInRange(AudioRules.Normalize.LowestVolume_Min, AudioRules.Normalize.LowestVolume_Max)]
        [JsonProperty(Names.LowestVolume)]
        public float LowestVolume { get; set; }

        /// <summary> Ceiling on how much gain may be applied. </summary>
        [RuleInRange(AudioRules.Normalize.MaximumAmp_Min, AudioRules.Normalize.MaximumAmp_Max)]
        [JsonProperty(Names.MaximumAmp)]
        public float MaximumAmp { get; set; }

        public AudioNormalize()
        {
            FadeInTime = AudioRules.Normalize.FadeInTime_Default;
            LowestVolume = AudioRules.Normalize.LowestVolume_Default;
            MaximumAmp = AudioRules.Normalize.MaximumAmp_Default;
        }
        public AudioNormalize(float mixLevel, float fadeInTime,
            float lowestVolume, float maximumAmp) : base(mixLevel)
        {
            FadeInTime = fadeInTime;
            LowestVolume = lowestVolume;
            MaximumAmp = maximumAmp;
        }
    }
}