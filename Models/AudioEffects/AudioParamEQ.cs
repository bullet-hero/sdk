using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.AudioEffects
{
    /// <summary>
    /// One parametric EQ band: boost or cut a chosen frequency region. Unlike the Low/Highpass pair
    /// it can add level, not only remove it.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class AudioParamEQ : AudioEffect, IModel<AudioParamEQ>
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
    }
}