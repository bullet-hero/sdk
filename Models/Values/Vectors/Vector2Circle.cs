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
    /// A 2D vector rolled inside a disc around a center. Direction-neutral, unlike Vector2Rect whose
    /// corners bias the spread diagonally.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class Vector2Circle : IVector2, IModel<Vector2Circle>
    {
        /// <summary> Center X the disc is built around. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.CoordX)]
        public float X { get; set; }

        /// <summary> Center Y the disc is built around. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.CoordY)]
        public float Y { get; set; }

        /// <summary> Maximum distance from the center. Zero collapses the value to the center point. </summary>
        [RuleInRange(ValueRules.FloatZero, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.Radius)]
        public float Radius { get; set; }
        
        public Vector2Circle()
        {
            X = ValueRules.FloatZero;
            Y = ValueRules.FloatZero;
            Radius = ValueRules.FloatOne;
        }
        public Vector2Circle(float x, float y, float radius)
        {
            X = x;
            Y = y;
            Radius = radius;
        }

        public VectorType GetModelType() => VectorType.RandomCircle;
    }
}