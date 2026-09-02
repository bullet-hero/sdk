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
    /// IntMinMax restricted to multiples of Step - e.g. "a random even count". Structurally the same
    /// idea as FloatMinMaxStep, just without fractional grids.
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(IntMinMaxStep.Min), nameof(IntMinMaxStep.Max))]
    [GenerateModel]
    public sealed partial class IntMinMaxStep : IInt, IModel<IntMinMaxStep>
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

        public IntType GetModelType() => IntType.RandomMinMaxStep;
    }
}