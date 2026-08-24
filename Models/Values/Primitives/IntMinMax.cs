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
    /// A whole number rolled inside a range - integer counterpart of FloatMinMax, re-rolled per
    /// evaluation rather than frozen at load.
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(IntMinMax.Min), nameof(IntMinMax.Max))]
    public class IntMinMax : IInt, IModel<IntMinMax>
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
        public void Reset()
        {
            Min = ValueRules.IntZero;
            Max = ValueRules.IntOne;
        }

        public IntType GetModelType() => IntType.RandomMinMax;
        
        public object Clone() => Copy();
        IInt ICopyable<IInt>.Copy() => new IntMinMax(Min, Max);
        public IntMinMax Copy() => new(Min, Max);

        public void Update(IntMinMax src)
        {
            Min = src.Min;
            Max = src.Max;
        }

        public void Pull(IntMinMax src)
        {
            Min = src.Min;
            Max = src.Max;
        }

        void IUpdatable<IInt>.Update(IInt src)
        {
            if (src is IntMinMax value) Update(value);
        }
        void IMoveable<IInt>.Pull(IInt src)
        {
            if (src is IntMinMax value) Pull(value);
        }

        public override bool Equals(object obj) => obj is IntMinMax value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(Min, Max);
        
        public bool Equals(IInt other) => other is IntMinMax value && Equals(value);
        public bool Equals(IntMinMax other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Min.Equals(other.Min)
                         && Max.Equals(other.Max);
            return result;
        }
    }
}