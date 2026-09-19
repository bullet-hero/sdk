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
    /// Vector2RectUniform snapped to a grid: the shared roll decides the point on the diagonal, then
    /// each axis is quantized to its own grid, so the step means the same distance on both.
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(Vector2RectStepUniform.MinX), nameof(Vector2RectStepUniform.MaxX))]
    [RulePropertyOrder(nameof(Vector2RectStepUniform.MinY), nameof(Vector2RectStepUniform.MaxY))]
    [GenerateModel]
    public sealed partial class Vector2RectStepUniform : IVector2, IModel<Vector2RectStepUniform>
    {
        /// <summary> Left edge, and the X origin the grid is measured from. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MinX)]
        public float MinX { get; set; }

        /// <summary> Bottom edge, and the Y origin the grid is measured from. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MinY)]
        public float MinY { get; set; }

        /// <summary> Right edge of the roll area. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MaxX)]
        public float MaxX { get; set; }

        /// <summary> Top edge of the roll area. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MaxY)]
        public float MaxY { get; set; }

        /// <summary> Cell size, shared by both axes - one square grid, not per-axis spacing. </summary>
        [RuleInRange(ValueRules.FloatZero, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.Step)]
        public float Step { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public Vector2RectStepUniform()
        {
            MinX = ValueRules.FloatZero;
            MinY = ValueRules.FloatZero;
            
            MaxX = ValueRules.FloatOne;
            MaxY = ValueRules.FloatOne;
            
            Step = ValueRules.FloatOne;
        }
        /// <summary> Every member at once, in declaration order. </summary>
        public Vector2RectStepUniform(float minX, float minY, float maxX, float maxY, float step)
        {
            MinX = minX;
            MinY = minY;
            
            MaxX = maxX;
            MaxY = maxY;
            
            Step = step;
        }

        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public VectorType GetModelType() => VectorType.RandomRectStepUniform;
    }
}