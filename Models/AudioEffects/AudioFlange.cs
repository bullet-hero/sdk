using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.AudioEffects
{
    /// <summary>
    /// Mixes the signal with a very short, sweeping delay of itself, producing the moving comb-filter
    /// whoosh. Same family as AudioChorus, but one modulated copy at a shorter delay.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class AudioFlange : AudioEffect, IModel<AudioFlange>
    {
        /// <summary> Level of the untouched signal. </summary>
        [RuleInRange(AudioRules.Flange.DryMix_Min, AudioRules.Flange.DryMix_Max)]
        [JsonProperty(Names.DryMix)]
        public float DryMix { get; set; }

        /// <summary> Level of the swept copy - the cancellation between the two is the effect. </summary>
        [RuleInRange(AudioRules.Flange.WetMix_Min, AudioRules.Flange.WetMix_Max)]
        [JsonProperty(Names.WetMix)]
        public float WetMix { get; set; }

        /// <summary> How far the delay swings. </summary>
        [RuleInRange(AudioRules.Flange.Depth_Min, AudioRules.Flange.Depth_Max)]
        [JsonProperty(Names.Depth)]
        public float Depth { get; set; }

        /// <summary> How fast the sweep repeats, in Hz. </summary>
        [RuleInRange(AudioRules.Flange.Rate_Min, AudioRules.Flange.Rate_Max)]
        [JsonProperty(Names.Rate)]
        public float Rate { get; set; }

        public AudioFlange()
        {
            DryMix = AudioRules.Flange.DryMix_Default;
            WetMix = AudioRules.Flange.WetMix_Default;
            Depth = AudioRules.Flange.Depth_Default;
            Rate = AudioRules.Flange.Rate_Default;
        }
        public AudioFlange(float mixLevel, float dryMix, float wetMix,
            float depth, float rate) : base(mixLevel)
        {
            DryMix = dryMix;
            WetMix = wetMix;
            Depth = depth;
            Rate = rate;
        }
    }
}