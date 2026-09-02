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
    /// Splits color channels toward the screen edges, like a cheap lens. Cheaper than the glitch
    /// effects and often enough to sell an "impact" moment on its own.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class ChromaticAberrationKey : PostProcessingKeyframe, IModel<ChromaticAberrationKey>
    {
        /// <summary> How far the channels separate. </summary>
        [RuleInRange(PostProcessingRules.ChromaticAberration.IntensityMin,
            PostProcessingRules.ChromaticAberration.IntensityMax)]
        [JsonProperty(Names.Intensity)]
        public float Intensity { get; set; }

        public ChromaticAberrationKey()
        {
            Intensity = 1.0f;
        }
        public ChromaticAberrationKey(float intensity,
            bool active, int frame, EaseType ease = Keyframe.DefaultEase) : base(active, frame, ease)
        {
            Intensity = intensity;
        }
    }
}