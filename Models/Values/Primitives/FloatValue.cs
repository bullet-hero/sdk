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

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public FloatValue()
        {
            Value = ValueRules.FloatZero;
        }
        /// <summary> Built from its value. </summary>
        public FloatValue(float value)
        {
            Value = value;
        }

        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public FloatType GetModelType() => FloatType.Value;
    }
}