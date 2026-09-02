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
    /// Shifts what counts as white, tinting the whole frame along two axes. A global mood shift that
    /// leaves relative colors intact - unlike LiftGammaGain, which regrades tonal ranges separately.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class WhiteBalanceKey : PostProcessingKeyframe, IModel<WhiteBalanceKey>
    {
        /// <summary> Blue-to-orange axis: negative cools the image, positive warms it. </summary>
        [RuleInRange(PostProcessingRules.WhiteBalance.TemperatureMin,
            PostProcessingRules.WhiteBalance.TemperatureMax)]
        [JsonProperty(Names.Temperature)]
        public float Temperature { get; set; }

        /// <summary> Green-to-magenta axis, perpendicular to Temperature. </summary>
        [RuleInRange(PostProcessingRules.WhiteBalance.TintMin,
            PostProcessingRules.WhiteBalance.TintMax)]
        [JsonProperty(Names.Tint)]
        public float Tint { get; set; }

        public WhiteBalanceKey()
        {
            Temperature = 0f;
            Tint = 0f;
        }
        public WhiteBalanceKey(float temperature, float tint,
            bool active, int frame, EaseType ease = Keyframe.DefaultEase) : base(active, frame, ease)
        {
            Temperature = temperature;
            Tint = tint;
        }
    }
}