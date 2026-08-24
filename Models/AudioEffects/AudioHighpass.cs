using System;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.AudioEffects
{
    /// <summary>
    /// Passes high frequencies and cuts everything below the cutoff - the "tiny speaker / radio"
    /// filter, and the mirror image of AudioLowpass.
    /// </summary>
    [RuleContainer]
    public class AudioHighpass : AudioEffect, IModel<AudioHighpass>
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
        public override void Reset()
        {
            base.Reset();
            CutoffFreq = AudioRules.Highpass.CutoffFreq_Default;
        }

        public override object Clone() => CopyImpl();
        public override AudioEffect Copy() => CopyImpl();
        AudioHighpass ICopyable<AudioHighpass>.Copy() => CopyImpl();

        private AudioHighpass CopyImpl() => new(MixLevel, CutoffFreq);

        public void Update(AudioHighpass src)
        {
            base.Update(src);

            CutoffFreq = src.CutoffFreq;
        }

        public void Pull(AudioHighpass src)
        {
            base.Pull(src);

            CutoffFreq = src.CutoffFreq;
        }

        public override bool Equals(object obj) => obj is AudioHighpass value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), CutoffFreq);

        public bool Equals(AudioHighpass other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other)
                         && CutoffFreq.Equals(other.CutoffFreq);
            return result;
        }
    }
}