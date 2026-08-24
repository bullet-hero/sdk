using System;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.AudioEffects
{
    /// <summary>
    /// Layers three slightly delayed, pitch-wobbled copies over the signal to make one source sound
    /// like several. Three taps is what separates it from the single-copy AudioFlange.
    /// </summary>
    [RuleContainer]
    public class AudioChorus : AudioEffect, IModel<AudioChorus>
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
        public override void Reset()
        {
            base.Reset();
            DryMix = AudioRules.Chorus.DryMix_Default;
            WetMixTap1 = AudioRules.Chorus.WetMixTap1_Default;
            WetMixTap2 = AudioRules.Chorus.WetMixTap2_Default;
            WetMixTap3 = AudioRules.Chorus.WetMixTap3_Default;
            Delay = AudioRules.Chorus.Delay_Default;
            Rate = AudioRules.Chorus.Rate_Default;
            Depth = AudioRules.Chorus.Depth_Default;
            Feedback = AudioRules.Chorus.Feedback_Default;
        }

        public override object Clone() => CopyImpl();
        public override AudioEffect Copy() => CopyImpl();
        AudioChorus ICopyable<AudioChorus>.Copy() => CopyImpl();

        private AudioChorus CopyImpl() => new(MixLevel, DryMix, WetMixTap1, WetMixTap2, WetMixTap3, Delay, Rate, Depth, Feedback);

        public void Update(AudioChorus src)
        {
            base.Update(src);

            DryMix = src.DryMix;
            WetMixTap1 = src.WetMixTap1;
            WetMixTap2 = src.WetMixTap2;
            WetMixTap3 = src.WetMixTap3;
            Delay = src.Delay;
            Rate = src.Rate;
            Depth = src.Depth;
            Feedback = src.Feedback;
        }

        public void Pull(AudioChorus src)
        {
            base.Pull(src);

            DryMix = src.DryMix;
            WetMixTap1 = src.WetMixTap1;
            WetMixTap2 = src.WetMixTap2;
            WetMixTap3 = src.WetMixTap3;
            Delay = src.Delay;
            Rate = src.Rate;
            Depth = src.Depth;
            Feedback = src.Feedback;
        }

        public override bool Equals(object obj) => obj is AudioChorus value && Equals(value);
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(base.GetHashCode());
            hashCode.Add(DryMix);
            hashCode.Add(WetMixTap1);
            hashCode.Add(WetMixTap2);
            hashCode.Add(WetMixTap3);
            hashCode.Add(Delay);
            hashCode.Add(Rate);
            hashCode.Add(Depth);
            hashCode.Add(Feedback);
            return hashCode.ToHashCode();
        }

        public bool Equals(AudioChorus other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other)
                         && DryMix.Equals(other.DryMix)
                         && WetMixTap1.Equals(other.WetMixTap1)
                         && WetMixTap2.Equals(other.WetMixTap2)
                         && WetMixTap3.Equals(other.WetMixTap3)
                         && Delay.Equals(other.Delay)
                         && Rate.Equals(other.Rate)
                         && Depth.Equals(other.Depth)
                         && Feedback.Equals(other.Feedback);
            return result;
        }
    }
}