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
    /// A 4-component vector rolled inside a hypersphere around a center - radial spread carried into
    /// 4D for consistency with the rest of the family, rather than because 4D geometry is authored.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class Vector4Circle : IVector4, IModel<Vector4Circle>
    {
        /// <summary> Center of the first component. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.CoordX)]
        public float X { get; set; }

        /// <summary> Center of the second component. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.CoordY)]
        public float Y { get; set; }

        /// <summary> Center of the third component. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.CoordZ)]
        public float Z { get; set; }

        /// <summary> Center of the fourth component. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.CoordW)]
        public float W { get; set; }

        /// <summary> Maximum distance from the center point. </summary>
        [RuleInRange(ValueRules.FloatZero, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.Radius)]
        public float Radius { get; set; }
        
        public Vector4Circle()
        {
            X = ValueRules.FloatZero;
            Y = ValueRules.FloatZero;
            Z = ValueRules.FloatZero;
            W = ValueRules.FloatZero;
            Radius = ValueRules.FloatOne;
        }
        public Vector4Circle(float x, float y, float z, float w, float radius)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
            Radius = radius;
        }

        public VectorType GetModelType() => VectorType.RandomCircle;
    }
}