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
    /// A 2D vector rolled anywhere inside an axis-aligned rectangle. Each axis rolls independently -
    /// use Vector2Circle instead when the spread should be radial rather than boxy.
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(Vector2Rect.MinX), nameof(Vector2Rect.MaxX))]
    [RulePropertyOrder(nameof(Vector2Rect.MinY), nameof(Vector2Rect.MaxY))]
    [GenerateModel]
    public sealed partial class Vector2Rect : IVector2, IModel<Vector2Rect>
    {
        // TODO add rule check for Min and Max, must be always Min < Max

        /// <summary> Left edge of the roll area. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MinX)]
        public float MinX { get; set; }

        /// <summary> Bottom edge of the roll area. </summary>
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

        public Vector2Rect()
        {
            MinX = ValueRules.FloatZero;
            MinY = ValueRules.FloatZero;
            
            MaxX = ValueRules.FloatOne;
            MaxY = ValueRules.FloatOne;
        }
        public Vector2Rect(float minX, float minY, float maxX, float maxY)
        {
            MinX = minX;
            MinY = minY;
            
            MaxX = maxX;
            MaxY = maxY;
        }

        public VectorType GetModelType() => VectorType.RandomRect;
    }
}