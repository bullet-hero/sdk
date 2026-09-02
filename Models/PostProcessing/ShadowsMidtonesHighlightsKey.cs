using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.PostProcessing
{
    /// <summary>
    /// Three-band color grading where the bands themselves are authorable: the two Limit fields say
    /// where shadows end and highlights begin, which LiftGammaGainKey cannot express.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class ShadowsMidtonesHighlightsKey : PostProcessingKeyframe, IModel<ShadowsMidtonesHighlightsKey>
    {
        /// <summary> Whether the shadow tint is applied. </summary>
        [JsonProperty(Names.Shadow)]
        public bool Shadows { get; set; }

        /// <summary> Tint applied to the dark band. </summary>
        [RuleNotNull(typeof(Color4Value))] // TODO add color hdr support for alpha rule (0f-2f)
        [JsonProperty(Names.ShadowColor)]
        public IColor4 ShadowsColor4 { get; set; }

        /// <summary> Whether the midtone tint is applied. </summary>
        [JsonProperty(Names.Midtone)]
        public bool Midtones { get; set; }

        /// <summary> Tint applied to the middle band - whatever falls between the two limits. </summary>
        [RuleNotNull(typeof(Color4Value))] // TODO add color hdr support for alpha rule (0f-2f)
        [JsonProperty(Names.MidtoneColor)]
        public IColor4 MidtonesColor4 { get; set; }

        /// <summary> Whether the highlight tint is applied. </summary>
        [JsonProperty(Names.Highlight)]
        public bool Highlights { get; set; }

        /// <summary> Tint applied to the bright band. </summary>
        [RuleNotNull(typeof(Color4Value))] // TODO add color hdr support for alpha rule (0f-2f)
        [JsonProperty(Names.HighlightColor)]
        public IColor4 HighlightsColor4 { get; set; }

        // TODO graph like in Post Processing menu

        /// <summary> Start/end luminance of the shadow band - a range, not a single cut, so shadows
        /// fade into midtones instead of banding. </summary>
        [RuleNotNull, RuleIVector2Ordered]
        [RuleIVector2InRange(PostProcessingRules.ShadowsMidtonesHighlights.ShadowLimitMin,
             PostProcessingRules.ShadowsMidtonesHighlights.ShadowLimitMax)]
        [JsonProperty(Names.ShadowLimit)]
        public IVector2 ShadowLimits { get; set; }

        /// <summary> Start/end luminance of the highlight band, same blended-edge idea. </summary>
        [RuleNotNull, RuleIVector2Ordered]
        [RuleIVector2InRange(PostProcessingRules.ShadowsMidtonesHighlights.HighlightLimitMin,
             PostProcessingRules.ShadowsMidtonesHighlights.HighlightLimitMax)]
        [JsonProperty(Names.HighlightLimit)]
        public IVector2 HighlightLimits { get; set; }

        public ShadowsMidtonesHighlightsKey()
        {
            Shadows = false;
            ShadowsColor4 = Color4Value.white;
            Midtones = false;
            MidtonesColor4 = Color4Value.white;
            Highlights = false;
            HighlightsColor4 = Color4Value.white;
            
            ShadowLimits = new Vector2Value(0f, 0.3f);
            HighlightLimits = new Vector2Value(0.55f, 1f);
        }
        public ShadowsMidtonesHighlightsKey(
            bool shadows, IColor4 shadowsColor4,
            bool midtones, IColor4 midtonesColor4, 
            bool highlights, IColor4 highlightsColor4, 
            IVector2 shadowLimits, IVector2 highlightLimits,
            bool active, int frame, EaseType ease = Keyframe.DefaultEase) : base(active, frame, ease)
        {
            Shadows = shadows;
            ShadowsColor4 = shadowsColor4;
            Midtones = midtones;
            MidtonesColor4 = midtonesColor4;
            Highlights = highlights;
            HighlightsColor4 = highlightsColor4;
            ShadowLimits = shadowLimits;
            HighlightLimits = highlightLimits;
        }
    }
}