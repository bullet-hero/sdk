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
    /// A number rolled anywhere in a continuous range. Stays random after loading - the roll happens
    /// per evaluation in the player, not once at load, which is what makes bullet patterns vary.
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(FloatMinMax.Min), nameof(FloatMinMax.Max))]
    public class FloatMinMax : IFloat, IModel<FloatMinMax>
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
        public void Reset()
        {
            Min = ValueRules.FloatZero;
            Max = ValueRules.FloatOne;
        }

        public FloatType GetModelType() => FloatType.RandomMinMax;
        
        public object Clone() => Copy();
        IFloat ICopyable<IFloat>.Copy() => new FloatMinMax(Min, Max);
        public FloatMinMax Copy() => new(Min, Max);

        public void Update(FloatMinMax src)
        {
            Min = src.Min;
            Max = src.Max;
        }

        public void Pull(FloatMinMax src)
        {
            Min = src.Min;
            Max = src.Max;
        }

        void IUpdatable<IFloat>.Update(IFloat src)
        {
            if (src is FloatMinMax value) Update(value);
        }
        void IMoveable<IFloat>.Pull(IFloat src)
        {
            if (src is FloatMinMax value) Pull(value);
        }

        public override bool Equals(object obj) => obj is FloatMinMax value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(Min, Max);
        
        public bool Equals(IFloat other) => other is FloatMinMax value && Equals(value);
        public bool Equals(FloatMinMax other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Min.Equals(other.Min)
                         && Max.Equals(other.Max);
            return result;
        }
    }
}