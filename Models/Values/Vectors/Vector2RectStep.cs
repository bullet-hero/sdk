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
    /// Vector2Rect snapped to a grid - random placement that still lands on cells, which is how a
    /// scattered pattern stays visually aligned.
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(Vector2RectStep.MinX), nameof(Vector2RectStep.MaxX))]
    [RulePropertyOrder(nameof(Vector2RectStep.MinY), nameof(Vector2RectStep.MaxY))]
    [GenerateModel]
    public sealed partial class Vector2RectStep : IVector2, IModel<Vector2RectStep>
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

        public Vector2RectStep()
        {
            MinX = ValueRules.FloatZero;
            MinY = ValueRules.FloatZero;
            
            MaxX = ValueRules.FloatOne;
            MaxY = ValueRules.FloatOne;
            
            Step = ValueRules.FloatOne;
        }
        public Vector2RectStep(float minX, float minY, float maxX, float maxY, float step)
        {
            MinX = minX;
            MinY = minY;
            
            MaxX = maxX;
            MaxY = maxY;
            
            Step = step;
        }

        public VectorType GetModelType() => VectorType.RandomRectStep;
    }
}