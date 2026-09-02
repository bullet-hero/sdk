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
    /// Smears the frame along camera motion. Quality and clamp are deliberately not authorable -
    /// quality is the player's setting and the clamp is fixed, so a level cannot make it unreadable.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class MotionBlurKey : PostProcessingKeyframe, IModel<MotionBlurKey> // HEAVY IN ANY CASE, PHONES DON'T LIKE IT
    {
        // Quality (client settings variable, he set it himself)

        /// <summary> Strength of the smear. </summary>
        [RuleInRange(PostProcessingRules.MotionBlur.IntensityMin,
            PostProcessingRules.MotionBlur.IntensityMax)]
        [JsonProperty(Names.Intensity)]
        public float Intensity { get; set; }
        
        // Clamp (0.2f, predefined)

        public MotionBlurKey()
        {
            Intensity = 1f;
        }
        public MotionBlurKey(float intensity, 
            bool active, int frame, EaseType ease = Keyframe.DefaultEase) : base(active, frame, ease)
        {
            Intensity = intensity;
        }
    }
}