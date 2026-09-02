using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.AudioEffects
{
    /// <summary>
    /// Base of every DSP effect on a track. Carries the one field they all share - how loudly the
    /// processed signal is mixed back in - which doubles as the effect's on/off switch.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public partial class AudioEffect : IModel<AudioEffect>
    {
        /// <summary> Wet-signal level in dB. At the disabled floor the effect is silent, which is
        /// how "enabled" is encoded - there is deliberately no separate bool per effect. </summary>
        [RuleInRange(AudioRules.MixLevel_Disabled, AudioRules.MixLevel_Enabled)]
        [JsonProperty(Names.MixLevel)]
        public float MixLevel { get; set; }

        public AudioEffect()
        {
            MixLevel = AudioRules.MixLevel_Default;
        }
        public AudioEffect(float mixLevel)
        {
            MixLevel = mixLevel;
        }
    }
}