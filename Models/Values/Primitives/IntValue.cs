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
    /// A plain authored whole number - the literal IInt variant, used where a value must be countable
    /// (particle counts, indices) rather than interpolatable.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class IntValue : IInt, IModel<IntValue>
    {
        /// <summary> The number itself. </summary>
        [RuleInRange(ValueRules.MinIntValue, ValueRules.MaxIntValue)]
        [JsonProperty(Names.ValueShort)]
        public int Value { get; set; }

        public IntValue()
        {
            Value = ValueRules.IntZero;
        }
        public IntValue(int value)
        {
            Value = value;
        }

        public IntType GetModelType() => IntType.Value;
    }
}