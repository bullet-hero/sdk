using System;
using System.Collections.Generic;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Values
{
    /// <summary>
    /// A color ramp stored as data - the color counterpart of CurveValue, used for particle tint over
    /// life or speed. RGB and alpha are two independent tracks, so a fade can outlive a hue shift.
    /// </summary>
    [RuleContainer]
    public class GradientValue : IModel<GradientValue>
    {
        /// <summary> RGB stops along the ramp. </summary>
        [RuleNotNull, RuleCollectionNoNullItems]
        [RuleCollectionMinCount(ValueRules.MinGradientKeys), RuleCollectionMaxCount(ValueRules.MaxGradientKeys)]
        [RuleCollectionSorted(nameof(GradientColorKeyValue.Time))]
        [JsonProperty(Names.ColorKeys)]
        public List<GradientColorKeyValue> ColorKeys { get; set; }

        /// <summary> Opacity stops, placed at their own times independently of ColorKeys. </summary>
        [RuleNotNull, RuleCollectionNoNullItems]
        [RuleCollectionMinCount(ValueRules.MinGradientKeys), RuleCollectionMaxCount(ValueRules.MaxGradientKeys)]
        [RuleCollectionSorted(nameof(GradientAlphaKeyValue.Time))]
        [JsonProperty(Names.AlphaKeys)]
        public List<GradientAlphaKeyValue> AlphaKeys { get; set; }

        /// <summary> Blend or hard-step between stops - the difference between a smooth fade and a
        /// banded palette. </summary>
        [RuleEnumValid(GradientInterpolationMode.PerceptualBlend)]
        [JsonProperty(Names.Mode)]
        public GradientInterpolationMode Mode { get; set; }

        /// <summary> Space the blend happens in (linear vs. gamma); changes the midpoints, not the
        /// stops themselves. </summary>
        [RuleEnumValid(GradientColorSpace.Linear)]
        [JsonProperty(Names.ColorSpace)]
        public GradientColorSpace ColorSpace { get; set; }
        
        public GradientValue()
        {
            ColorKeys = new List<GradientColorKeyValue>();
            AlphaKeys = new List<GradientAlphaKeyValue>();
            Mode = GradientInterpolationMode.PerceptualBlend;
            ColorSpace = GradientColorSpace.Linear;
        }
        public GradientValue(List<GradientColorKeyValue> colorKeys, List<GradientAlphaKeyValue> alphaKeys,
            GradientInterpolationMode mode, GradientColorSpace colorSpace)
        {
            ColorKeys = colorKeys;
            AlphaKeys = alphaKeys;
            Mode = mode;
            ColorSpace = colorSpace;
        }
        public void Reset()
        {
            ColorKeys.Clear();
            AlphaKeys.Clear();
            Mode = GradientInterpolationMode.PerceptualBlend;
            ColorSpace = GradientColorSpace.Linear;
        }

        public object Clone() => Copy();
        public GradientValue Copy() => new(ColorKeys.CopyList(), AlphaKeys.CopyList(), Mode, ColorSpace);

        public void Update(GradientValue src)
        {
            ColorKeys = src.ColorKeys.CopyList();
            AlphaKeys = src.AlphaKeys.CopyList();
            Mode = src.Mode;
            ColorSpace = src.ColorSpace;
        }

        public void Pull(GradientValue src)
        {
            ColorKeys = src.ColorKeys.CopyList();
            AlphaKeys = src.AlphaKeys.CopyList();
            Mode = src.Mode;
            ColorSpace = src.ColorSpace;
        }

        public override bool Equals(object obj) => obj is GradientValue value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(ColorKeys.GetListHashCode(),
            AlphaKeys.GetListHashCode(), (int)Mode, (int)ColorSpace);

        public bool Equals(GradientValue other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = ColorKeys.ListEquals(other.ColorKeys)
                         && AlphaKeys.ListEquals(other.AlphaKeys)
                         && Mode == other.Mode
                         && ColorSpace == other.ColorSpace;
            return result;
        }
    }
}