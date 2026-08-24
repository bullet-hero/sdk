using System;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Values;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.PostProcessing
{
    /// <summary>
    /// Classic three-way color grading by tonal range. Each range has its own on/off flag, so a level
    /// can tint only highlights and leave the rest untouched - the mathematical counterpart of
    /// ShadowsMidtonesHighlightsKey, which grades by perceptual bands instead.
    /// </summary>
    [RuleContainer]
    public class LiftGammaGainKey : PostProcessingKeyframe, IModel<LiftGammaGainKey>
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
        public override void Reset()
        {
            base.Reset();
            Lift = false;
            LiftColor4 = Color4Value.white;
            Gamma = false;
            GammaColor4 = Color4Value.white;
            Gain = false;
            GainColor4 = Color4Value.white;
        }

        public override object Clone() => CopyImpl();
        public override PostProcessingKeyframe Copy() => CopyImpl();
        LiftGammaGainKey ICopyable<LiftGammaGainKey>.Copy() => CopyImpl();
        
        private LiftGammaGainKey CopyImpl() => new(Lift, LiftColor4.Copy(), Gamma, GammaColor4.Copy(), Gain, GainColor4.Copy(), Active, Frame, Ease);

        public void Update(LiftGammaGainKey src)
        {
            base.Update(src);

            Lift = src.Lift;
            LiftColor4 = src.LiftColor4.Copy();
            Gamma = src.Gamma;
            GammaColor4 = src.GammaColor4.Copy();
            Gain = src.Gain;
            GainColor4 = src.GainColor4.Copy();
        }

        public void Pull(LiftGammaGainKey src)
        {
            base.Pull(src);

            Lift = src.Lift;
            LiftColor4 = LiftColor4.PullFrom(src.LiftColor4);
            Gamma = src.Gamma;
            GammaColor4 = GammaColor4.PullFrom(src.GammaColor4);
            Gain = src.Gain;
            GainColor4 = GainColor4.PullFrom(src.GainColor4);
        }

        public override bool Equals(object obj) => obj is LiftGammaGainKey value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(),
            Lift, LiftColor4, Gamma, GammaColor4, Gain, GainColor4);

        public bool Equals(LiftGammaGainKey other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other)
                         && Lift == other.Lift
                         && LiftColor4.Equals(other.LiftColor4)
                         && Gamma == other.Gamma
                         && GammaColor4.Equals(other.GammaColor4)
                         && Gain == other.Gain
                         && GainColor4.Equals(other.GainColor4);
            return result;
        }
    }
}