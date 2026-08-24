using System;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.AudioEffects
{
    /// <summary>
    /// Mixes the signal with a very short, sweeping delay of itself, producing the moving comb-filter
    /// whoosh. Same family as AudioChorus, but one modulated copy at a shorter delay.
    /// </summary>
    [RuleContainer]
    public class AudioFlange : AudioEffect, IModel<AudioFlange>
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
        public override void Reset()
        {
            base.Reset();
            DryMix = AudioRules.Flange.DryMix_Default;
            WetMix = AudioRules.Flange.WetMix_Default;
            Depth = AudioRules.Flange.Depth_Default;
            Rate = AudioRules.Flange.Rate_Default;
        }

        public override object Clone() => CopyImpl();
        public override AudioEffect Copy() => CopyImpl();
        AudioFlange ICopyable<AudioFlange>.Copy() => CopyImpl();

        private AudioFlange CopyImpl() => new(MixLevel, DryMix, WetMix, Depth, Rate);

        public void Update(AudioFlange src)
        {
            base.Update(src);

            DryMix = src.DryMix;
            WetMix = src.WetMix;
            Depth = src.Depth;
            Rate = src.Rate;
        }

        public void Pull(AudioFlange src)
        {
            base.Pull(src);

            DryMix = src.DryMix;
            WetMix = src.WetMix;
            Depth = src.Depth;
            Rate = src.Rate;
        }

        public override bool Equals(object obj) => obj is AudioFlange value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), DryMix, WetMix, Depth, Rate);

        public bool Equals(AudioFlange other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other)
                         && DryMix.Equals(other.DryMix)
                         && WetMix.Equals(other.WetMix)
                         && Depth.Equals(other.Depth)
                         && Rate.Equals(other.Rate);
            return result;
        }
    }
}