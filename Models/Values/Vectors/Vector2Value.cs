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
    /// A literal 2D vector - the workhorse of the format: positions, sizes, pivots and anchors all
    /// land here when the author picked exact numbers rather than a random variant.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class Vector2Value : IVector2, IModel<Vector2Value>
    {
        /// <summary> Horizontal component. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.CoordX)]
        public float X { get; set; }

        /// <summary> Vertical component. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.CoordY)]
        public float Y { get; set; }

        public static Vector2Value Zero => new(0.0f, 0.0f);
        public static Vector2Value One => new(1.0f, 1.0f);
        
        public static Vector2Value Right => new(1.0f, 0.0f);
        public static Vector2Value Left => new(-1.0f, 0.0f);
        public static Vector2Value Up => new(0.0f, 1.0f);
        public static Vector2Value Down => new(0.0f, -1.0f);
        
        public Vector2Value()
        {
            X = ValueRules.FloatZero;
            Y = ValueRules.FloatZero;
        }
        public Vector2Value(float x, float y)
        {
            X = x;
            Y = y;
        }

        public VectorType GetModelType() => VectorType.Value;
    }
}