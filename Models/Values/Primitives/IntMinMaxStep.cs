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
    /// IntMinMax restricted to multiples of Step - e.g. "a random even count". Structurally the same
    /// idea as FloatMinMaxStep, just without fractional grids.
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(IntMinMaxStep.Min), nameof(IntMinMaxStep.Max))]
    public class IntMinMaxStep : IInt, IModel<IntMinMaxStep>
    {
        /// <summary> Inclusive lower bound, and the origin the grid is measured from. </summary>
        [RuleInRange(ValueRules.MinIntValue, ValueRules.MaxIntValue)]
        [JsonProperty(Names.Min)]
        public int Min { get; set; }

        /// <summary> Inclusive upper bound of the roll. </summary>
        [RuleInRange(ValueRules.MinIntValue, ValueRules.MaxIntValue)]
        [JsonProperty(Names.Max)]
        public int Max { get; set; }

        /// <summary> Grid spacing. Non-negative; zero degenerates back to a plain roll. </summary>
        [RuleInRange(ValueRules.IntZero, ValueRules.MaxIntValue)]
        [JsonProperty(Names.Step)]
        public int Step { get; set; }

        public IntMinMaxStep()
        {
            Min = ValueRules.IntZero;
            Max = ValueRules.IntOne;
            Step = ValueRules.IntOne;
        }
        public IntMinMaxStep(int min, int max, int step)
        {
            Min = min;
            Max = max;
            Step = step;
        }
        public void Reset()
        {
            Min = ValueRules.IntZero;
            Max = ValueRules.IntOne;
            Step = ValueRules.IntOne;
        }

        public IntType GetModelType() => IntType.RandomMinMaxStep;

        public object Clone() => Copy();
        IInt ICopyable<IInt>.Copy() => new IntMinMaxStep(Min, Max, Step);
        public IntMinMaxStep Copy() => new(Min, Max, Step);

        public void Update(IntMinMaxStep src)
        {
            Min = src.Min;
            Max = src.Max;
            Step = src.Step;
        }

        public void Pull(IntMinMaxStep src)
        {
            Min = src.Min;
            Max = src.Max;
            Step = src.Step;
        }

        void IUpdatable<IInt>.Update(IInt src)
        {
            if (src is IntMinMaxStep value) Update(value);
        }
        void IMoveable<IInt>.Pull(IInt src)
        {
            if (src is IntMinMaxStep value) Pull(value);
        }

        public override bool Equals(object obj) => obj is IntMinMaxStep value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(Min, Max, Step);
        
        public bool Equals(IInt other) => other is IntMinMaxStep value && Equals(value);
        public bool Equals(IntMinMaxStep other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Min.Equals(other.Min)
                         && Max.Equals(other.Max)
                         && Step.Equals(other.Step);
            return result;
        }
    }
}