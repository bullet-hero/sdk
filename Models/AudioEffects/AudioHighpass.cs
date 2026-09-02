using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.AudioEffects
{
    /// <summary>
    /// Passes high frequencies and cuts everything below the cutoff - the "tiny speaker / radio"
    /// filter, and the mirror image of AudioLowpass.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class AudioHighpass : AudioEffect, IModel<AudioHighpass>
    {
        /// <summary> Frequency in Hz below which content is attenuated; at the minimum the filter is
        /// effectively transparent. </summary>
        [RuleInRange(AudioRules.Highpass.CutoffFreq_Min, AudioRules.Highpass.CutoffFreq_Max)]
        [JsonProperty(Names.CutoffFreq)]
        public float CutoffFreq { get; set; }

        public AudioHighpass()
        {
            CutoffFreq = AudioRules.Highpass.CutoffFreq_Default;
        }
        public AudioHighpass(float mixLevel, float cutoffFreq) : base(mixLevel)
        {
            CutoffFreq = cutoffFreq;
        }
    }
}