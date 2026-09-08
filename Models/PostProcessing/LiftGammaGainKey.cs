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