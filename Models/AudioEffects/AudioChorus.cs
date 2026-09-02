using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.AudioEffects
{
    /// <summary>
    /// Layers three slightly delayed, pitch-wobbled copies over the signal to make one source sound
    /// like several. Three taps is what separates it from the single-copy AudioFlange.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class AudioChorus : AudioEffect, IModel<AudioChorus>
    {
        /// <summary> Level of the untouched signal. </summary>
        [RuleInRange(AudioRules.Chorus.DryMix_Min, AudioRules.Chorus.DryMix_Max)]
        [JsonProperty(Names.DryMix)]
        public float DryMix { get; set; }

        /// <summary> Level of the first delayed copy. </summary>
        [RuleInRange(AudioRules.Chorus.WetMixTap1_Min, AudioRules.Chorus.WetMixTap1_Max)]
        [JsonProperty(Names.WetMixTap1)]
        public float WetMixTap1 { get; set; }

        /// <summary> Level of the second copy - the taps are spaced apart, so their balance decides
        /// how wide the chorus feels. </summary>
        [RuleInRange(AudioRules.Chorus.WetMixTap2_Min, AudioRules.Chorus.WetMixTap2_Max)]
        [JsonProperty(Names.WetMixTap2)]
        public float WetMixTap2 { get; set; }

        /// <summary> Level of the third copy. </summary>
        [RuleInRange(AudioRules.Chorus.WetMixTap3_Min, AudioRules.Chorus.WetMixTap3_Max)]
        [JsonProperty(Names.WetMixTap3)]
        public float WetMixTap3 { get; set; }

        /// <summary> Base delay of the copies in ms - short enough to be heard as thickness rather
        /// than as an echo. </summary>
        [RuleInRange(AudioRules.Chorus.Delay_Min, AudioRules.Chorus.Delay_Max)]
        [JsonProperty(Names.Delay)]
        public float Delay { get; set; }

        /// <summary> How fast the delay is modulated, in Hz. </summary>
        [RuleInRange(AudioRules.Chorus.Rate_Min, AudioRules.Chorus.Rate_Max)]
        [JsonProperty(Names.Rate)]
        public float Rate { get; set; }

        /// <summary> How far the delay swings - Rate sets the speed of the wobble, this its size. </summary>
        [RuleInRange(AudioRules.Chorus.Depth_Min, AudioRules.Chorus.Depth_Max)]
        [JsonProperty(Names.Depth)]
        public float Depth { get; set; }

        /// <summary> How much output is fed back into the effect, deepening it toward flanger
        /// territory. </summary>
        [RuleInRange(AudioRules.Chorus.Feedback_Min, AudioRules.Chorus.Feedback_Max)]
        [JsonProperty(Names.Feedback)]
        public float Feedback { get; set; }

        public AudioChorus()
        {
            DryMix = AudioRules.Chorus.DryMix_Default;
            WetMixTap1 = AudioRules.Chorus.WetMixTap1_Default;
            WetMixTap2 = AudioRules.Chorus.WetMixTap2_Default;
            WetMixTap3 = AudioRules.Chorus.WetMixTap3_Default;
            Delay = AudioRules.Chorus.Delay_Default;
            Rate = AudioRules.Chorus.Rate_Default;
            Depth = AudioRules.Chorus.Depth_Default;
            Feedback = AudioRules.Chorus.Feedback_Default;
        }
        public AudioChorus(float mixLevel, float dryMix,
            float wetMixTap1, float wetMixTap2, float wetMixTap3,
            float delay, float rate, float depth, float feedback) 
            : base(mixLevel)
        {
            DryMix = dryMix;
            WetMixTap1 = wetMixTap1;
            WetMixTap2 = wetMixTap2;
            WetMixTap3 = wetMixTap3;
            Delay = delay;
            Rate = rate;
            Depth = depth;
            Feedback = feedback;
        }
    }
}