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
    // The eight curves are URP's own; each one's meaning is LutBuilder's formula, and a scalar beside a
    // curve is two answers for one knob. Docs/POST_PROCESSING.md

    /// <summary>
    /// The level's colour grading curves - URP's ColorCurves, all eight. Null means the author never touched
    /// it, which is neutral: identity for the four YRGB curves, a flat 0.5 for the other four.
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

        // Both ends ClampForever, load bearing: WrapMode.Default behaves as Once, which wraps a sample at
        // exactly the last key time. Core's ColorCurvesGroup forces the same. Docs/POST_PROCESSING.md

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