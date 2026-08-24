using System;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.AudioEffects
{
    /// <summary>
    /// Passes low frequencies and cuts everything above the cutoff - the "underwater/behind a wall"
    /// filter.
    /// </summary>
    [RuleContainer]
    public class AudioLowpass : AudioEffect, IModel<AudioLowpass>
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
        public override void Reset()
        {
            base.Reset();
            CutoffFreq = AudioRules.Lowpass.CutoffFreq_Default;
        }

        public override object Clone() => CopyImpl();
        public override AudioEffect Copy() => CopyImpl();
        AudioLowpass ICopyable<AudioLowpass>.Copy() => CopyImpl();

        private AudioLowpass CopyImpl() => new(MixLevel, CutoffFreq);

        public void Update(AudioLowpass src)
        {
            base.Update(src);

            CutoffFreq = src.CutoffFreq;
        }

        public void Pull(AudioLowpass src)
        {
            base.Pull(src);

            CutoffFreq = src.CutoffFreq;
        }

        public override bool Equals(object obj) => obj is AudioLowpass value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), CutoffFreq);

        public bool Equals(AudioLowpass other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other)
                         && CutoffFreq.Equals(other.CutoffFreq);
            return result;
        }
    }
}