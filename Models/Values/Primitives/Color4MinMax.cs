using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Values
{
    /// <summary>
    /// RGBA counterpart of Color3MinMax - alpha rolls independently too, which is what makes
    /// "random, sometimes barely visible" possible from a single value.
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(Color4MinMax.MinR), nameof(Color4MinMax.MaxR))]
    [RulePropertyOrder(nameof(Color4MinMax.MinG), nameof(Color4MinMax.MaxG))]
    [RulePropertyOrder(nameof(Color4MinMax.MinB), nameof(Color4MinMax.MaxB))]
    [RulePropertyOrder(nameof(Color4MinMax.MinA), nameof(Color4MinMax.MaxA))]
    [GenerateModel]
    public sealed partial class Color4MinMax : IColor4, IModel<Color4MinMax>
    {
        /// <summary> Lower bound of the red roll. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.MinR)]
        public float MinR { get; set; }

        /// <summary> Lower bound of the green roll. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.MinG)]
        public float MinG { get; set; }

        /// <summary> Lower bound of the blue roll. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.MinB)]
        public float MinB { get; set; }

        /// <summary> Lower bound of the opacity roll. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.MinA)]
        public float MinA { get; set; }

        /// <summary> Upper bound of the red roll. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.MaxR)]
        public float MaxR { get; set; }

        /// <summary> Upper bound of the green roll. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.MaxG)]
        public float MaxG { get; set; }

        /// <summary> Upper bound of the blue roll. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.MaxB)]
        public float MaxB { get; set; }

        /// <summary> Upper bound of the opacity roll. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.MaxA)]
        public float MaxA { get; set; }

        public Color4MinMax()
        {
            MinR = ValueRules.MinColor;
            MinG = ValueRules.MinColor;
            MinB = ValueRules.MinColor;
            MinA = ValueRules.MinColor;
            
            MaxR = ValueRules.MaxColor;
            MaxG = ValueRules.MaxColor;
            MaxB = ValueRules.MaxColor;
            MaxA = ValueRules.MaxColor;
        }
        public Color4MinMax(float minR, float minG, float minB, float minA, 
            float maxR, float maxG, float maxB, float maxA)
        {
            MinR = minR;
            MinG = minG;
            MinB = minB;
            MinA = minA;
            
            MaxR = maxR;
            MaxG = maxG;
            MaxB = maxB;
            MaxA = maxA;
        }

        public ColorType GetModelType() => ColorType.RandomMinMax;
    }
}