using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.PostProcessing
{
    // THE EIGHT CURVES ARE URP'S OWN EIGHT, AND WHAT EACH ONE MEANS IS THE LutBuilder'S FORMULA:
    // `satMult *= curve(x) * 2` for the three saturation curves (neutral 0.5, i.e. a multiply by
    // one), `hue += curve(hue) - 0.5` for HueVsHue (neutral 0.5, the full 0..1 range being a whole
    // turn), and `c = curve(c)` for the four YRGB ones, which are an absolute mapping and are
    // therefore neutral at IDENTITY rather than at any constant. Every axis is 0..1 on both sides,
    // and every value outside that is what the 128x1 curve texture clamps away.
    //
    // This used to be two floats - a HueVsHue and a SatVsSat - and they could not be kept beside
    // the curves, because a scalar IS a flat curve and two answers for one knob is what the rest of
    // this format refuses. They could not be extended either: the formula above is why three
    // constants multiplying satMult are one constant, and why a constant YRGB curve maps every
    // input to a single output, i.e. paints the frame one colour. Only a curve reshapes anything.
    // A global hue/saturation SLIDER is a real authoring want and remains unserved here on purpose
    // - it is what URP's ColorAdjustments component is for, and this project has not wired that
    // track up yet. It belongs there, not as a second answer on this one.

    /// <summary>
    /// The level's colour grading curves - URP's ColorCurves, all eight of them. Each is null when
    /// the author never touched it, which is a real state and not missing data: null means neutral,
    /// and neutral is identity for the four YRGB curves and a flat 0.5 for the other four.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class ColorCurvesKey : PostProcessingKeyframe, IModel<ColorCurvesKey>
    {
        /// <summary> Luminance across the whole image - applied to each channel in turn, before the
        /// per-channel curves below. </summary>
        [JsonProperty(Names.CurveMaster)]
        public CurveValue Master { get; set; }

        /// <summary> Red channel intensity, applied after Master. </summary>
        [JsonProperty(Names.CurveRed)]
        public CurveValue Red { get; set; }

        /// <summary> Green channel intensity, applied after Master. </summary>
        [JsonProperty(Names.CurveGreen)]
        public CurveValue Green { get; set; }

        /// <summary> Blue channel intensity, applied after Master. </summary>
        [JsonProperty(Names.CurveBlue)]
        public CurveValue Blue { get; set; }

        /// <summary> Hue shift, indexed by the input hue - the one curve that rotates the palette
        /// rather than reweighting it. </summary>
        [JsonProperty(Names.HueVsHue)]
        public CurveValue HueVsHue { get; set; }

        /// <summary> Saturation, indexed by the input hue - what desaturates one colour and leaves
        /// the rest alone. </summary>
        [JsonProperty(Names.HueVsSat)]
        public CurveValue HueVsSat { get; set; }

        /// <summary> Saturation, indexed by the input saturation - pulls the already-vivid up or
        /// the near-grey down. </summary>
        [JsonProperty(Names.SatVsSat)]
        public CurveValue SatVsSat { get; set; }

        /// <summary> Saturation, indexed by luminance - the one that treats highlights and shadows
        /// differently. </summary>
        [JsonProperty(Names.LumVsSat)]
        public CurveValue LumVsSat { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public ColorCurvesKey()
        {
        }

        /// <summary> Built from its active, frame and keyframe default ease. </summary>
        public ColorCurvesKey(bool active, int frame, EaseType ease = Keyframe.DefaultEase)
            : base(active, frame, ease)
        {
        }

        /// <summary> Every member at once, in declaration order. </summary>
        public ColorCurvesKey(CurveValue master, CurveValue red, CurveValue green, CurveValue blue,
            CurveValue hueVsHue, CurveValue hueVsSat, CurveValue satVsSat, CurveValue lumVsSat,
            bool active, int frame, EaseType ease = Keyframe.DefaultEase) : base(active, frame, ease)
        {
            Master = master;
            Red = red;
            Green = green;
            Blue = blue;
            HueVsHue = hueVsHue;
            HueVsSat = hueVsSat;
            SatVsSat = satVsSat;
            LumVsSat = lumVsSat;
        }

        // BOTH ENDS CLAMP, AND THAT IS NOT A PREFERENCE. A colour curve is sampled on exactly 0..1
        // - both of URP's axes are - so nothing ever asks what lies outside it, and the wrap mode is
        // the one field on a CurveValue that means nothing here. It is still load bearing:
        // CurveWrapMode.Default maps to UnityEngine.WrapMode.Default, which behaves as Once, and
        // Once answers a sample at EXACTLY the last key time by wrapping back to the first - so an
        // identity curve read 0 at x = 1 and painted the top of the ramp black. Core's
        // ColorCurvesGroup forces the same two modes onto an AUTHORED curve for the same reason.

        /// <summary> What a null YRGB curve means, as data - the identity mapping, tangents included,
        /// so it reads back as the straight line it is rather than as an S curve through two points. </summary>
        public static CurveValue CreateIdentity() => new(new()
        {
            new CurveKeyframeValue(ValueRules.MinCurveTime, PostProcessingRules.ColorCurves.CurveMin,
                CurveWeightedMode.None, CurveTangentMode.Free, 1f, 1f, 0f, 0f),
            new CurveKeyframeValue(ValueRules.MaxCurveTime, PostProcessingRules.ColorCurves.CurveMax,
                CurveWeightedMode.None, CurveTangentMode.Free, 1f, 1f, 0f, 0f),
        }, CurveWrapMode.ClampForever, CurveWrapMode.ClampForever);

        /// <summary> What a null hue/saturation curve means, as data - flat at the neutral value. </summary>
        public static CurveValue CreateNeutral() => new(new()
        {
            new CurveKeyframeValue(ValueRules.MinCurveTime, PostProcessingRules.ColorCurves.CurveNeutral),
            new CurveKeyframeValue(ValueRules.MaxCurveTime, PostProcessingRules.ColorCurves.CurveNeutral),
        }, CurveWrapMode.ClampForever, CurveWrapMode.ClampForever);
    }
}