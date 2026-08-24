using System;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.AudioEffects
{
    /// <summary>
    /// Pulls loud parts down so quiet parts can sit higher - keeps a track audible under a busy mix
    /// without clipping. Shapes dynamics; AudioNormalize targets overall level instead.
    /// </summary>
    [RuleContainer]
    public class AudioCompressor : AudioEffect, IModel<AudioCompressor>
    {
        /// <summary> Level in dB above which compression starts acting. </summary>
        [RuleInRange(AudioRules.Compressor.Threshold_Min, AudioRules.Compressor.Threshold_Max)]
        [JsonProperty(Names.Threshold)]
        public float Threshold { get; set; }

        /// <summary> How quickly it clamps down after a peak; too fast kills transients. </summary>
        [RuleInRange(AudioRules.Compressor.Attack_Min, AudioRules.Compressor.Attack_Max)]
        [JsonProperty(Names.Attack)]
        public float Attack { get; set; }

        /// <summary> How quickly it lets go afterwards; too fast pumps audibly. </summary>
        [RuleInRange(AudioRules.Compressor.Release_Min, AudioRules.Compressor.Release_Max)]
        [JsonProperty(Names.Release)]
        public float Release { get; set; }

        /// <summary> Gain added back after compression, to recover the level it removed. </summary>
        [RuleInRange(AudioRules.Compressor.MakeUpGain_Min, AudioRules.Compressor.MakeUpGain_Max)]
        [JsonProperty(Names.MakeUpGain)]
        public float MakeUpGain { get; set; }

        public AudioCompressor()
        {
            Threshold = AudioRules.Compressor.Threshold_Default;
            Attack = AudioRules.Compressor.Attack_Default;
            Release = AudioRules.Compressor.Release_Default;
            MakeUpGain = AudioRules.Compressor.MakeUpGain_Default;
        }
        public AudioCompressor(float mixLevel, float threshold,
            float attack, float release, float makeUpGain) : base(mixLevel)
        {
            Threshold = threshold;
            Attack = attack;
            Release = release;
            MakeUpGain = makeUpGain;
        }
        public override void Reset()
        {
            base.Reset();
            Threshold = AudioRules.Compressor.Threshold_Default;
            Attack = AudioRules.Compressor.Attack_Default;
            Release = AudioRules.Compressor.Release_Default;
            MakeUpGain = AudioRules.Compressor.MakeUpGain_Default;
        }

        public override object Clone() => CopyImpl();
        public override AudioEffect Copy() => CopyImpl();
        AudioCompressor ICopyable<AudioCompressor>.Copy() => CopyImpl();

        private AudioCompressor CopyImpl() => new(MixLevel, Threshold, Attack, Release, MakeUpGain);

        public void Update(AudioCompressor src)
        {
            base.Update(src);

            Threshold = src.Threshold;
            Attack = src.Attack;
            Release = src.Release;
            MakeUpGain = src.MakeUpGain;
        }

        public void Pull(AudioCompressor src)
        {
            base.Pull(src);

            Threshold = src.Threshold;
            Attack = src.Attack;
            Release = src.Release;
            MakeUpGain = src.MakeUpGain;
        }

        public override bool Equals(object obj) => obj is AudioCompressor value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Threshold, Attack, Release, MakeUpGain);

        public bool Equals(AudioCompressor other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other)
                         && Threshold.Equals(other.Threshold)
                         && Attack.Equals(other.Attack)
                         && Release.Equals(other.Release)
                         && MakeUpGain.Equals(other.MakeUpGain);
            return result;
        }
    }
}