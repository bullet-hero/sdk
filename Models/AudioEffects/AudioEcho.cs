using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.AudioEffects
{
    /// <summary>
    /// Repeats the signal at a fixed interval, each repeat quieter than the last. Rhythmic and
    /// countable, unlike AudioReverb's diffuse tail.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class AudioEcho : AudioEffect, IModel<AudioEcho>
    {
        /// <summary> Milliseconds between repeats - tie it to the song's tempo to keep echoes on
        /// the beat. </summary>
        [RuleInRange(AudioRules.Echo.Delay_Min, AudioRules.Echo.Delay_Max)]
        [JsonProperty(Names.Delay)]
        public float Delay { get; set; }

        /// <summary> How much of each repeat feeds the next; higher values ring on longer. </summary>
        [RuleInRange(AudioRules.Echo.Decay_Min, AudioRules.Echo.Decay_Max)]
        [JsonProperty(Names.Decay)]
        public float Decay { get; set; }

        /// <summary> Channel cap for the echo buffer. </summary>
        [RuleInRange(AudioRules.Echo.MaxChannels_Min, AudioRules.Echo.MaxChannels_Max)]
        [JsonProperty(Names.MaxChannels)]
        public float MaxChannels { get; set; }

        /// <summary> Level of the untouched signal in the output. </summary>
        [RuleInRange(AudioRules.Echo.DryMix_Min, AudioRules.Echo.DryMix_Max)]
        [JsonProperty(Names.DryMix)]
        public float DryMix { get; set; }

        /// <summary> Level of the echoed signal - balanced against DryMix, separately from the
        /// inherited MixLevel. </summary>
        [RuleInRange(AudioRules.Echo.WetMix_Min, AudioRules.Echo.WetMix_Max)]
        [JsonProperty(Names.WetMix)]
        public float WetMix { get; set; }

        public AudioEcho()
        {
            Delay = AudioRules.Echo.Delay_Default;
            Decay = AudioRules.Echo.Decay_Default;
            MaxChannels = AudioRules.Echo.MaxChannels_Default;
            DryMix = AudioRules.Echo.DryMix_Default;
            WetMix = AudioRules.Echo.WetMix_Default;
        }
        public AudioEcho(float mixLevel, float delay, float decay,
            float maxChannels, float dryMix, float wetMix) : base(mixLevel)
        {
            Delay = delay;
            Decay = decay;
            MaxChannels = maxChannels;
            DryMix = dryMix;
            WetMix = wetMix;
        }
    }
}