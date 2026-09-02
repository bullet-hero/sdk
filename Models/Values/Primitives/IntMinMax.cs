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
    /// A whole number rolled inside a range - integer counterpart of FloatMinMax, re-rolled per
    /// evaluation rather than frozen at load.
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(IntMinMax.Min), nameof(IntMinMax.Max))]
    [GenerateModel]
    public sealed partial class IntMinMax : IInt, IModel<IntMinMax>
    {
        /// <summary> Inclusive lower bound of the roll. </summary>
        [RuleInRange(ValueRules.MinIntValue, ValueRules.MaxIntValue)]
        [JsonProperty(Names.Min)]
        public int Min { get; set; }

        /// <summary> Inclusive upper bound of the roll. </summary>
        [RuleInRange(ValueRules.MinIntValue, ValueRules.MaxIntValue)]
        [JsonProperty(Names.Max)]
        public int Max { get; set; }

        public IntMinMax()
        {
            Min = ValueRules.IntZero;
            Max = ValueRules.IntOne;
        }
        public IntMinMax(int min, int max)
        {
            Min = min;
            Max = max;
        }

        public IntType GetModelType() => IntType.RandomMinMax;
    }
}