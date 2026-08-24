using System;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Values
{
    /// <summary>
    /// A plain authored number - the IFloat variant that means exactly what it says, as opposed to
    /// the two random ones. The default choice everywhere a float is keyframeable.
    /// </summary>
    [RuleContainer]
    public class FloatValue : IFloat, IModel<FloatValue>
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
        public void Reset()
        {
            Value = ValueRules.FloatZero;
        }

        public FloatType GetModelType() => FloatType.Value;
        
        public object Clone() => Copy();
        IFloat ICopyable<IFloat>.Copy() => new FloatValue(Value);
        public FloatValue Copy() => new(Value);
        
        public void Update(FloatValue src)
        {
            Value = src.Value;
        }

        public void Pull(FloatValue src)
        {
            Value = src.Value;
        }

        void IUpdatable<IFloat>.Update(IFloat src)
        {
            if (src is FloatValue value) Update(value);
        }
        void IMoveable<IFloat>.Pull(IFloat src)
        {
            if (src is FloatValue value) Pull(value);
        }

        public bool Equals(IFloat other) => other is FloatValue value && Equals(value);
        public bool Equals(FloatValue other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Value.Equals(other.Value);
            return result;
        }

        public override bool Equals(object obj) => obj is FloatValue value && Equals(value);
        public override int GetHashCode() => Value.GetHashCode();
    }
}