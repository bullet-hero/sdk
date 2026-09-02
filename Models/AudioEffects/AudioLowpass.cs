using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.AudioEffects
{
    /// <summary>
    /// Passes low frequencies and cuts everything above the cutoff - the "underwater/behind a wall"
    /// filter.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class AudioLowpass : AudioEffect, IModel<AudioLowpass>
    {
        /// <summary> Frequency in Hz above which content is attenuated; at the maximum the filter is
        /// effectively transparent. </summary>
        [RuleInRange(AudioRules.Lowpass.CutoffFreq_Min, AudioRules.Lowpass.CutoffFreq_Max)]
        [JsonProperty(Names.CutoffFreq)]
        public float CutoffFreq { get; set; }

        public AudioLowpass()
        {
            CutoffFreq = AudioRules.Lowpass.CutoffFreq_Default;
        }
        public AudioLowpass(float mixLevel, float cutoffFreq) : base(mixLevel)
        {
            CutoffFreq = cutoffFreq;
        }
    }
}