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
    /// Hue and saturation remapping. Currently a stub of URP's real curve grid: two scalars instead
    /// of editable curves (see the TODO below), so it can shift the palette but not reshape it.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class ColorCurvesKey : PostProcessingKeyframe, IModel<ColorCurvesKey>
    {
        /// <summary> Global hue shift - rotates every color around the wheel by the same amount. </summary>
        [RuleInRange(PostProcessingRules.ColorCurves.HueVsHueMin,
            PostProcessingRules.ColorCurves.HueVsHueMax)]
        [JsonProperty(Names.HueVsHue)]
        public float HueVsHue { get; set; }

        /// <summary> Global saturation scale, from grayscale to oversaturated. </summary>
        [RuleInRange(PostProcessingRules.ColorCurves.SatVsSatMin,
            PostProcessingRules.ColorCurves.SatVsSatMax)]
        [JsonProperty(Names.SatVsSat)]
        public float SatVsSat { get; set; }
        
        // TODO add more values, especially for curves

        public ColorCurvesKey()
        {
            HueVsHue = 0.5f;
            SatVsSat = 0.5f;
        }
        public ColorCurvesKey(float hueVsHue, float satVsSat,
            bool active, int frame, EaseType ease = Keyframe.DefaultEase) : base(active, frame, ease)
        {
            HueVsHue = hueVsHue;
            SatVsSat = satVsSat;
        }
    }
}