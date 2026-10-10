using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Values;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.PostProcessing
{
    // Three channels: URP reads a fourth as a signed OFFSET (neutral 0), not an alpha. Why, and why no
    // version moved: Docs/POST_PROCESSING.md

    /// <summary>
    /// Classic three-way color grading by tonal range, each range with its own on/off flag. The counterpart
    /// of ShadowsMidtonesHighlightsKey, which grades by perceptual bands instead.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class LiftGammaGainKey : PostProcessingKeyframe, IModel<LiftGammaGainKey>
    {
        /// <summary> Whether the lift (dark end) correction is applied. </summary>
        [JsonProperty(Names.Lift)]
        public bool Lift { get; set; }

        /// <summary> Color pushed into the darkest tones - offsets the black point. </summary>
        [RuleNotNull(typeof(Color3Value))]
        [JsonProperty(Names.LiftColor)]
        public IColor3 LiftColor3 { get; set; }

        /// <summary> Whether the gamma (midtone) correction is applied. </summary>
        [JsonProperty(Names.Gamma)]
        public bool Gamma { get; set; }

        /// <summary> Color applied to midtones, leaving black and white ends anchored. </summary>
        [RuleNotNull(typeof(Color3Value))]
        [JsonProperty(Names.GammaColor)]
        public IColor3 GammaColor3 { get; set; }

        /// <summary> Whether the gain (bright end) correction is applied. </summary>
        [JsonProperty(Names.Gain)]
        public bool Gain { get; set; }

        /// <summary> Color multiplied into the brightest tones - scales the white point. </summary>
        [RuleNotNull(typeof(Color3Value))]
        [JsonProperty(Names.GainColor)]
        public IColor3 GainColor3 { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public LiftGammaGainKey()
        {
            Lift = false;
            LiftColor3 = Color3Value.white;
            Gamma = false;
            GammaColor3 = Color3Value.white;
            Gain = false;
            GainColor3 = Color3Value.white;
        }
        /// <summary> Every member at once, in declaration order. </summary>
        public LiftGammaGainKey(
            bool lift, IColor3 liftColor3,
            bool gamma, IColor3 gammaColor3,
            bool gain, IColor3 gainColor3,
            bool active, int frame, EaseType ease = Keyframe.DefaultEase) : base(active, frame, ease)
        {
            Lift = lift;
            LiftColor3 = liftColor3;
            Gamma = gamma;
            GammaColor3 = gammaColor3;
            Gain = gain;
            GainColor3 = gainColor3;
        }
    }
}