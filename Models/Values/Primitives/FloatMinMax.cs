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
    /// A number rolled anywhere in a continuous range. Stays random after loading - the roll happens
    /// per evaluation in the player, not once at load, which is what makes bullet patterns vary.
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(FloatMinMax.Min), nameof(FloatMinMax.Max))]
    [GenerateModel]
    public sealed partial class FloatMinMax : IFloat, IModel<FloatMinMax>
    {
        /// <summary> Inclusive lower bound of the roll. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.Min)]
        public float Min { get; set; }

        /// <summary> Inclusive upper bound of the roll. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.Max)]
        public float Max { get; set; }

        public FloatMinMax()
        {
            Min = ValueRules.FloatZero;
            Max = ValueRules.FloatOne;
        }
        public FloatMinMax(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public FloatType GetModelType() => FloatType.RandomMinMax;
    }
}