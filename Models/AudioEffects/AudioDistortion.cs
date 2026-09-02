using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.AudioEffects
{
    /// <summary>
    /// Clips the waveform for a gritty, overdriven sound. The simplest effect here - one dial.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class AudioDistortion : AudioEffect, IModel<AudioDistortion>
    {
        /// <summary> How hard the signal is clipped. </summary>
        [RuleInRange(AudioRules.Distortion.Level_Min, AudioRules.Distortion.Level_Max)]
        [JsonProperty(Names.Level)]
        public float Level { get; set; }

        public AudioDistortion()
        {
            Level = AudioRules.Distortion.Level_Default;
        }
        public AudioDistortion(float mixLevel, float level) : base(mixLevel)
        {
            Level = level;
        }
    }
}