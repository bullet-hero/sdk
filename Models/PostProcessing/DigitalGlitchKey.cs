using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Keyframes;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.PostProcessing
{
    /// <summary>
    /// Corrupted-datastream artifacts: displaced blocks and color garbage. One dial, unlike the
    /// four-way AnalogGlitchKey, because block corruption has no separate axes to tune.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class DigitalGlitchKey : PostProcessingKeyframe, IModel<DigitalGlitchKey> // HEAVY IN ANY CASE, PHONES DON'T LIKE IT
    {
        /// <summary> How much of the frame gets corrupted. </summary>
        [RuleInRange(PostProcessingRules.DigitalGlitch.IntensityMin,
            PostProcessingRules.DigitalGlitch.IntensityMax)]
        [JsonProperty(Names.Intensity)]
        public float Intensity { get; set; }

        public DigitalGlitchKey()
        {
            Intensity = 0.1f;
        }
        public DigitalGlitchKey(float intensity,
            bool active, int frame, EaseType ease = Keyframe.DefaultEase) : base(active, frame, ease)
        {
            Intensity = intensity;
        }
    }
}