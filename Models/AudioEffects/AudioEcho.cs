using System;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.AudioEffects
{
    /// <summary>
    /// Repeats the signal at a fixed interval, each repeat quieter than the last. Rhythmic and
    /// countable, unlike AudioReverb's diffuse tail.
    /// </summary>
    [RuleContainer]
    public class AudioEcho : AudioEffect, IModel<AudioEcho>
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
        public override void Reset()
        {
            base.Reset();
            Delay = AudioRules.Echo.Delay_Default;
            Decay = AudioRules.Echo.Decay_Default;
            MaxChannels = AudioRules.Echo.MaxChannels_Default;
            DryMix = AudioRules.Echo.DryMix_Default;
            WetMix = AudioRules.Echo.WetMix_Default;
        }

        public override object Clone() => CopyImpl();
        public override AudioEffect Copy() => CopyImpl();
        AudioEcho ICopyable<AudioEcho>.Copy() => CopyImpl();

        private AudioEcho CopyImpl() => new(MixLevel, Delay, Decay, MaxChannels, DryMix, WetMix);

        public void Update(AudioEcho src)
        {
            base.Update(src);

            Delay = src.Delay;
            Decay = src.Decay;
            MaxChannels = src.MaxChannels;
            DryMix = src.DryMix;
            WetMix = src.WetMix;
        }

        public void Pull(AudioEcho src)
        {
            base.Pull(src);

            Delay = src.Delay;
            Decay = src.Decay;
            MaxChannels = src.MaxChannels;
            DryMix = src.DryMix;
            WetMix = src.WetMix;
        }

        public override bool Equals(object obj) => obj is AudioEcho value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Delay, Decay, MaxChannels, DryMix, WetMix);

        public bool Equals(AudioEcho other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other)
                         && Delay.Equals(other.Delay)
                         && Decay.Equals(other.Decay)
                         && MaxChannels.Equals(other.MaxChannels)
                         && DryMix.Equals(other.DryMix)
                         && WetMix.Equals(other.WetMix);
            return result;
        }
    }
}