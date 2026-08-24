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
    /// A 4-component vector rolled inside a hypersphere around a center - radial spread carried into
    /// 4D for consistency with the rest of the family, rather than because 4D geometry is authored.
    /// </summary>
    [RuleContainer]
    public class Vector4Circle : IVector4, IModel<Vector4Circle>
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
        public void Reset()
        {
            X = ValueRules.FloatZero;
            Y = ValueRules.FloatZero;
            Z = ValueRules.FloatZero;
            W = ValueRules.FloatZero;
            Radius = ValueRules.FloatOne;
        }

        public VectorType GetModelType() => VectorType.RandomCircle;

        public object Clone() => Copy();
        IVector4 ICopyable<IVector4>.Copy() => new Vector4Circle(X, Y, Z, W, Radius);
        public Vector4Circle Copy() => new(X, Y, Z, W, Radius);

        public void Update(Vector4Circle src)
        {
            X = src.X;
            Y = src.Y;
            Z = src.Z;
            W = src.W;
            Radius = src.Radius;
        }

        public void Pull(Vector4Circle src)
        {
            X = src.X;
            Y = src.Y;
            Z = src.Z;
            W = src.W;
            Radius = src.Radius;
        }

        void IUpdatable<IVector4>.Update(IVector4 src)
        {
            if (src is Vector4Circle value) Update(value);
        }
        void IMoveable<IVector4>.Pull(IVector4 src)
        {
            if (src is Vector4Circle value) Pull(value);
        }

        public override bool Equals(object obj) => obj is Vector4Circle value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z, W, Radius);
        
        public bool Equals(IVector4 other) => other is Vector4Circle value && Equals(value);
        public bool Equals(Vector4Circle other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = X.Equals(other.X)
                         && Y.Equals(other.Y)
                         && Z.Equals(other.Z)
                         && W.Equals(other.W)
                         && Radius.Equals(other.Radius);
            return result;
        }
    }
}