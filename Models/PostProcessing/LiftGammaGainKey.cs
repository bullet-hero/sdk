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
    /// <summary>
    /// Classic three-way color grading by tonal range. Each range has its own on/off flag, so a level
    /// can tint only highlights and leave the rest untouched - the mathematical counterpart of
    /// ShadowsMidtonesHighlightsKey, which grades by perceptual bands instead.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class LiftGammaGainKey : PostProcessingKeyframe, IModel<LiftGammaGainKey>
    {
        /// <summary> Whether the lift (dark end) correction is applied. </summary>
        [JsonProperty(Names.Lift)]
        public bool Lift { get; set; }

        /// <summary> Color pushed into the darkest tones - offsets the black point. </summary>
        [RuleNotNull(typeof(Color4Value))] // TODO add color hdr support for alpha rule (0f-2f)
        [JsonProperty(Names.LiftColor)]
        public IColor4 LiftColor4 { get; set; }

        /// <summary> Whether the gamma (midtone) correction is applied. </summary>
        [JsonProperty(Names.Gamma)]
        public bool Gamma { get; set; }

        /// <summary> Color applied to midtones, leaving black and white ends anchored. </summary>
        [RuleNotNull(typeof(Color4Value))] // TODO add color hdr support for alpha rule (0f-2f)
        [JsonProperty(Names.GammaColor)]
        public IColor4 GammaColor4 { get; set; }

        /// <summary> Whether the gain (bright end) correction is applied. </summary>
        [JsonProperty(Names.Gain)]
        public bool Gain { get; set; }

        /// <summary> Color multiplied into the brightest tones - scales the white point. </summary>
        [RuleNotNull(typeof(Color4Value))] // TODO add color hdr support for alpha rule (0f-2f)
        [JsonProperty(Names.GainColor)]
        public IColor4 GainColor4 { get; set; }

        public LiftGammaGainKey()
        {
            Lift = false;
            LiftColor4 = Color4Value.white;
            Gamma = false;
            GammaColor4 = Color4Value.white;
            Gain = false;
            GainColor4 = Color4Value.white;
        }
        public LiftGammaGainKey(
            bool lift, IColor4 liftColor4,
            bool gamma, IColor4 gammaColor4,
            bool gain, IColor4 gainColor4,
            bool active, int frame, EaseType ease = Keyframe.DefaultEase) : base(active, frame, ease)
        {
            Lift = lift;
            LiftColor4 = liftColor4;
            Gamma = gamma;
            GammaColor4 = gammaColor4;
            Gain = gain;
            GainColor4 = gainColor4;
        }
    }
}