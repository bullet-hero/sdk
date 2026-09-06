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
    // THE FOURTH COMPONENT WAS NEVER AN ALPHA, and carrying one is what this stopped doing. URP
    // takes each of these as a Vector4 whose w is a signed OFFSET, not opacity: PrepareLiftGammaGain
    // adds it to every channel after removing the luminance (gamma additionally does w += 1 and
    // inverts), and PrepareShadowsMidtonesHighlights weights it - times one when negative, times
    // FOUR when positive - and adds that. Its neutral is zero.
    //
    // The model stored a four-component colour defaulting to white, so w arrived as 1: turning any
    // of these ranges on with an untouched colour pushed +4 into every channel of a whole tonal
    // band. The colour picker called it Alpha, nothing said otherwise, and the neutral value was
    // unreachable except by guessing. So the component is gone rather than documented - these carry
    // three channels now and the provider sends URP the zero the effect is neutral at.
    //
    // A LEVEL WRITTEN BEFORE THIS READS BACK FINE and no version moved, which is a decision about
    // where the project is rather than about the format: the payload simply carries one property
    // more than the type has, Newtonsoft drops it, and the colour survives. What does not survive is
    // whatever the old alpha was doing to the picture - which is the point.

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
        [RuleNotNull(typeof(Color3Value))]
        [JsonProperty(Names.ShadowColor)]
        public IColor3 ShadowsColor3 { get; set; }

        /// <summary> Whether the midtone tint is applied. </summary>
        [JsonProperty(Names.Midtone)]
        public bool Midtones { get; set; }

        /// <summary> Tint applied to the middle band - whatever falls between the two limits. </summary>
        [RuleNotNull(typeof(Color3Value))]
        [JsonProperty(Names.MidtoneColor)]
        public IColor3 MidtonesColor3 { get; set; }

        /// <summary> Whether the highlight tint is applied. </summary>
        [JsonProperty(Names.Highlight)]
        public bool Highlights { get; set; }

        /// <summary> Tint applied to the bright band. </summary>
        [RuleNotNull(typeof(Color3Value))]
        [JsonProperty(Names.HighlightColor)]
        public IColor3 HighlightsColor3 { get; set; }

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
            ShadowsColor3 = Color3Value.white;
            Midtones = false;
            MidtonesColor3 = Color3Value.white;
            Highlights = false;
            HighlightsColor3 = Color3Value.white;
            
            ShadowLimits = new Vector2Value(0f, 0.3f);
            HighlightLimits = new Vector2Value(0.55f, 1f);
        }
        public ShadowsMidtonesHighlightsKey(
            bool shadows, IColor3 shadowsColor3,
            bool midtones, IColor3 midtonesColor3, 
            bool highlights, IColor3 highlightsColor3, 
            IVector2 shadowLimits, IVector2 highlightLimits,
            bool active, int frame, EaseType ease = Keyframe.DefaultEase) : base(active, frame, ease)
        {
            Shadows = shadows;
            ShadowsColor3 = shadowsColor3;
            Midtones = midtones;
            MidtonesColor3 = midtonesColor3;
            Highlights = highlights;
            HighlightsColor3 = highlightsColor3;
            ShadowLimits = shadowLimits;
            HighlightLimits = highlightLimits;
        }
    }
}