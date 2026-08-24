using System;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Values
{
    /// <summary>
    /// A literal 4-component vector. Not a position - it is the generic "four numbers that travel
    /// together" carrier (UV rects, shader-style params), reached through Vector4Key.
    /// </summary>
    [RuleContainer]
    public class Vector4Value : IVector4, IModel<Vector4Value>
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

        public static Vector4Value Zero => new(0.0f, 0.0f, 0.0f, 0.0f);
        public static Vector4Value One => new(1.0f, 1.0f, 1.0f, 1.0f);
        
        public Vector4Value()
        {
            X = ValueRules.FloatZero;
            Y = ValueRules.FloatZero;
            Z = ValueRules.FloatZero;
            W = ValueRules.FloatZero;
        }
        public Vector4Value(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }
        public void Reset()
        {
            X = ValueRules.FloatZero;
            Y = ValueRules.FloatZero;
            Z = ValueRules.FloatZero;
            W = ValueRules.FloatZero;
        }

        public VectorType GetModelType() => VectorType.Value;
        
        public object Clone() => Copy();
        IVector4 ICopyable<IVector4>.Copy() => new Vector4Value(X, Y, Z, W);
        public Vector4Value Copy() => new(X, Y, Z, W);

        public void Update(Vector4Value src)
        {
            X = src.X;
            Y = src.Y;
            Z = src.Z;
            W = src.W;
        }

        public void Pull(Vector4Value src)
        {
            X = src.X;
            Y = src.Y;
            Z = src.Z;
            W = src.W;
        }

        void IUpdatable<IVector4>.Update(IVector4 src)
        {
            if (src is Vector4Value value) Update(value);
        }
        void IMoveable<IVector4>.Pull(IVector4 src)
        {
            if (src is Vector4Value value) Pull(value);
        }

        public override bool Equals(object obj) => obj is Vector4Value value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);
        
        public bool Equals(IVector4 other) => other is Vector4Value value && Equals(value);
        public bool Equals(Vector4Value other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = X.Equals(other.X)
                         && Y.Equals(other.Y)
                         && Z.Equals(other.Z)
                         && W.Equals(other.W);
            return result;
        }
    }
}