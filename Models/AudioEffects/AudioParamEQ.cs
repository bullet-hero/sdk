using System;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.AudioEffects
{
    /// <summary>
    /// One parametric EQ band: boost or cut a chosen frequency region. Unlike the Low/Highpass pair
    /// it can add level, not only remove it.
    /// </summary>
    [RuleContainer]
    public class AudioParamEQ : AudioEffect, IModel<AudioParamEQ>
    {
        /// <summary> Frequency in Hz the band is centered on. </summary>
        [RuleInRange(AudioRules.ParamEQ.CenterFreq_Min, AudioRules.ParamEQ.CenterFreq_Max)]
        [JsonProperty(Names.CenterFreq)]
        public float CenterFreq { get; set; }

        /// <summary> Width of the band in octaves - narrow for surgical fixes, wide for tone shaping. </summary>
        [RuleInRange(AudioRules.ParamEQ.OctaveRange_Min, AudioRules.ParamEQ.OctaveRange_Max)]
        [JsonProperty(Names.OctaveRange)]
        public float OctaveRange { get; set; }

        /// <summary> Gain applied inside the band; below 1 cuts, above 1 boosts. </summary>
        [RuleInRange(AudioRules.ParamEQ.FrequencyGain_Min, AudioRules.ParamEQ.FrequencyGain_Max)]
        [JsonProperty(Names.FreqGain)]
        public float FrequencyGain { get; set; }

        public AudioParamEQ()
        {
            CenterFreq = AudioRules.ParamEQ.CenterFreq_Default;
            OctaveRange = AudioRules.ParamEQ.OctaveRange_Default;
            FrequencyGain = AudioRules.ParamEQ.FrequencyGain_Default;
        }
        public AudioParamEQ(float mixLevel, float centerFreq,
            float octaveRange, float frequencyGain) : base(mixLevel)
        {
            CenterFreq = centerFreq;
            OctaveRange = octaveRange;
            FrequencyGain = frequencyGain;
        }
        public override void Reset()
        {
            base.Reset();
            CenterFreq = AudioRules.ParamEQ.CenterFreq_Default;
            OctaveRange = AudioRules.ParamEQ.OctaveRange_Default;
            FrequencyGain = AudioRules.ParamEQ.FrequencyGain_Default;
        }

        public override object Clone() => CopyImpl();
        public override AudioEffect Copy() => CopyImpl();
        AudioParamEQ ICopyable<AudioParamEQ>.Copy() => CopyImpl();

        private AudioParamEQ CopyImpl() => new(MixLevel, CenterFreq, OctaveRange, FrequencyGain);

        public void Update(AudioParamEQ src)
        {
            base.Update(src);

            CenterFreq = src.CenterFreq;
            OctaveRange = src.OctaveRange;
            FrequencyGain = src.FrequencyGain;
        }

        public void Pull(AudioParamEQ src)
        {
            base.Pull(src);

            CenterFreq = src.CenterFreq;
            OctaveRange = src.OctaveRange;
            FrequencyGain = src.FrequencyGain;
        }

        public override bool Equals(object obj) => obj is AudioParamEQ value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), CenterFreq, OctaveRange, FrequencyGain);

        public bool Equals(AudioParamEQ other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other)
                         && CenterFreq.Equals(other.CenterFreq)
                         && OctaveRange.Equals(other.OctaveRange)
                         && FrequencyGain.Equals(other.FrequencyGain);
            return result;
        }
    }
}