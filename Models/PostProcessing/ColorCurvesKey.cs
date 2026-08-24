using System;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Keyframes;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.PostProcessing
{
    /// <summary>
    /// Hue and saturation remapping. Currently a stub of URP's real curve grid: two scalars instead
    /// of editable curves (see the TODO below), so it can shift the palette but not reshape it.
    /// </summary>
    [RuleContainer]
    public class ColorCurvesKey : PostProcessingKeyframe, IModel<ColorCurvesKey>
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
        public override void Reset()
        {
            base.Reset();
            HueVsHue = 0.5f;
            SatVsSat = 0.5f;
        }
        
        public override object Clone() => CopyImpl();
        public override PostProcessingKeyframe Copy() => CopyImpl();
        ColorCurvesKey ICopyable<ColorCurvesKey>.Copy() => CopyImpl();
        
        private ColorCurvesKey CopyImpl() => new(HueVsHue, SatVsSat, Active, Frame, Ease);

        public void Update(ColorCurvesKey src)
        {
            base.Update(src);

            HueVsHue = src.HueVsHue;
            SatVsSat = src.SatVsSat;
        }

        public void Pull(ColorCurvesKey src)
        {
            base.Pull(src);

            HueVsHue = src.HueVsHue;
            SatVsSat = src.SatVsSat;
        }

        public override bool Equals(object obj) => obj is ColorCurvesKey value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), HueVsHue, SatVsSat);

        public bool Equals(ColorCurvesKey other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other)
                         && HueVsHue.Equals(other.HueVsHue)
                         && SatVsSat.Equals(other.SatVsSat);
            return result;
        }
    }
}