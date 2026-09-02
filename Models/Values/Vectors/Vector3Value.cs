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
    /// A literal 3D vector. The game is 2D, so this shows up where a third axis is genuinely
    /// meaningful - effect forces (EffectObjectForces orbital axis) rather than object transforms.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class Vector3Value : IVector3, IModel<Vector3Value>
    {
        /// <summary> Horizontal component. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.CoordX)]
        public float X { get; set; }

        /// <summary> Vertical component. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.CoordY)]
        public float Y { get; set; }

        /// <summary> Depth component - the axis 2D gameplay ignores but rotation/orbit math needs. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.CoordZ)]
        public float Z { get; set; }
        
        public static Vector3Value Zero => new(0.0f, 0.0f, 0.0f);
        public static Vector3Value One => new(1.0f, 1.0f, 1.0f);
        
        public static Vector3Value Right => new(1.0f, 0.0f, 0.0f);
        public static Vector3Value Left => new(-1.0f, 0.0f, 0.0f);
        public static Vector3Value Up => new(0.0f, 1.0f, 0.0f);
        public static Vector3Value Down => new(0.0f, -1.0f, 0.0f);
        public static Vector3Value Forward => new(0.0f, 0.0f, 1.0f);
        public static Vector3Value Backward => new(0.0f, 0.0f, -1.0f);
        
        public Vector3Value()
        {
            X = ValueRules.FloatZero;
            Y = ValueRules.FloatZero;
            Z = ValueRules.FloatZero;
        }
        public Vector3Value(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public VectorType GetModelType() => VectorType.Value;
    }
}