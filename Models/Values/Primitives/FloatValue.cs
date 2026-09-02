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
    /// A plain authored number - the IFloat variant that means exactly what it says, as opposed to
    /// the two random ones. The default choice everywhere a float is keyframeable.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class FloatValue : IFloat, IModel<FloatValue>
    {
        /// <summary> The number itself. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.ValueShort)]
        public float Value { get; set; }

        public FloatValue()
        {
            Value = ValueRules.FloatZero;
        }
        public FloatValue(float value)
        {
            Value = value;
        }

        public FloatType GetModelType() => FloatType.Value;
    }
}