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
    /// A literal 4-component vector. Not a position - it is the generic "four numbers that travel
    /// together" carrier (UV rects, shader-style params), reached through Vector4Key.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class Vector4Value : IVector4, IModel<Vector4Value>
    {
        /// <summary> First component. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.CoordX)]
        public float X { get; set; }

        /// <summary> Second component. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.CoordY)]
        public float Y { get; set; }

        /// <summary> Third component. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.CoordZ)]
        public float Z { get; set; }

        /// <summary> Fourth component. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.CoordW)]
        public float W { get; set; }

        /// <summary> All components zero. </summary>
        public static Vector4Value Zero => new(0.0f, 0.0f, 0.0f, 0.0f);
        /// <summary> All components one. </summary>
        public static Vector4Value One => new(1.0f, 1.0f, 1.0f, 1.0f);
        
        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public Vector4Value()
        {
            X = ValueRules.FloatZero;
            Y = ValueRules.FloatZero;
            Z = ValueRules.FloatZero;
            W = ValueRules.FloatZero;
        }
        /// <summary> Built from its x, y, z and w. </summary>
        public Vector4Value(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public VectorType GetModelType() => VectorType.Value;
    }
}