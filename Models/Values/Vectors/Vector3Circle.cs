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
    /// A 3D vector rolled inside a sphere around a center - the radial counterpart of Vector3Rect.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class Vector3Circle : IVector3, IModel<Vector3Circle>
    {
        /// <summary> Center X the sphere is built around. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.CoordX)]
        public float X { get; set; }

        /// <summary> Center Y the sphere is built around. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.CoordY)]
        public float Y { get; set; }

        /// <summary> Center Z the sphere is built around. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.CoordZ)]
        public float Z { get; set; }

        /// <summary> Maximum distance from the center; zero collapses to the center point. </summary>
        [RuleInRange(ValueRules.FloatZero, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.Radius)]
        public float Radius { get; set; }
        
        public Vector3Circle()
        {
            X = ValueRules.FloatZero;
            Y = ValueRules.FloatZero;
            Z = ValueRules.FloatZero;
            Radius = ValueRules.FloatOne;
        }
        public Vector3Circle(float x, float y, float z, float radius)
        {
            X = x;
            Y = y;
            Z = z;
            Radius = radius;
        }

        public VectorType GetModelType() => VectorType.RandomCircle;
    }
}