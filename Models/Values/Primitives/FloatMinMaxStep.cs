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
    /// FloatMinMax quantized to a grid - the roll can only land on Min + k*Step. Keeps randomness
    /// while staying on musically or visually meaningful values (quarter turns, whole beats).
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(FloatMinMaxStep.Min), nameof(FloatMinMaxStep.Max))]
    [GenerateModel]
    public sealed partial class FloatMinMaxStep : IFloat, IModel<FloatMinMaxStep>
    {
        /// <summary> Inclusive lower bound, and the origin the grid is measured from. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.Min)]
        public float Min { get; set; }

        /// <summary> Inclusive upper bound of the roll. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.Max)]
        public float Max { get; set; }

        /// <summary> Grid spacing. Non-negative; zero degenerates back to a plain continuous roll. </summary>
        [RuleInRange(ValueRules.FloatZero, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.Step)]
        public float Step { get; set; }

        public FloatMinMaxStep()
        {
            Min = ValueRules.FloatZero;
            Max = ValueRules.FloatOne;
            Step = ValueRules.FloatOne;
        }
        public FloatMinMaxStep(float min, float max, float step)
        {
            Min = min;
            Max = max;
            Step = step;
        }

        public FloatType GetModelType() => FloatType.RandomMinMaxStep;
    }
}