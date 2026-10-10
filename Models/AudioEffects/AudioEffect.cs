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
    /// processed signal is mixed back in, whose floor is silence.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public partial class AudioEffect : IModel<AudioEffect>
    {
        // MixLevel answers "how loud", not "is this effect here": an effect turned down to the floor
        // keeps its settings, one never touched is null on LevelTrackEffects.

        /// <summary> Wet-signal level in dB. At the floor the effect is silent but still in the
        /// chain, which is what AudioRules.IsActiveMixLevel asks about. </summary>
        [RuleInRange(AudioRules.MixLevel_Disabled, AudioRules.MixLevel_Enabled)]
        [JsonProperty(Names.MixLevel)]
        public float MixLevel { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public AudioEffect()
        {
            MixLevel = AudioRules.MixLevel_Default;
        }
        /// <summary> Built from its level. </summary>
        public AudioEffect(float mixLevel)
        {
            MixLevel = mixLevel;
        }
    }
}