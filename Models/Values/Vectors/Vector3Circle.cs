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
    /// A 3D vector rolled inside a sphere around a center - the radial counterpart of Vector3Rect.
    /// </summary>
    [RuleContainer]
    public class Vector3Circle : IVector3, IModel<Vector3Circle>
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
        public void Reset()
        {
            X = ValueRules.FloatZero;
            Y = ValueRules.FloatZero;
            Z = ValueRules.FloatZero;
            Radius = ValueRules.FloatOne;
        }

        public VectorType GetModelType() => VectorType.RandomCircle;

        public object Clone() => Copy();
        IVector3 ICopyable<IVector3>.Copy() => new Vector3Circle(X, Y, Z, Radius);
        public Vector3Circle Copy() => new(X, Y, Z, Radius);

        public void Update(Vector3Circle src)
        {
            X = src.X;
            Y = src.Y;
            Z = src.Z;
            Radius = src.Radius;
        }

        public void Pull(Vector3Circle src)
        {
            X = src.X;
            Y = src.Y;
            Z = src.Z;
            Radius = src.Radius;
        }

        void IUpdatable<IVector3>.Update(IVector3 src)
        {
            if (src is Vector3Circle value) Update(value);
        }
        void IMoveable<IVector3>.Pull(IVector3 src)
        {
            if (src is Vector3Circle value) Pull(value);
        }

        public override bool Equals(object obj) => obj is Vector3Circle value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z, Radius);
        
        public bool Equals(IVector3 other) => other is Vector3Circle value && Equals(value);
        public bool Equals(Vector3Circle other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = X.Equals(other.X)
                         && Y.Equals(other.Y)
                         && Z.Equals(other.Z)
                         && Radius.Equals(other.Radius);
            return result;
        }
    }
}