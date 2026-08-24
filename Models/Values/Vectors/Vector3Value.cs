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
    /// A literal 3D vector. The game is 2D, so this shows up where a third axis is genuinely
    /// meaningful - effect forces (EffectObjectForces orbital axis) rather than object transforms.
    /// </summary>
    [RuleContainer]
    public class Vector3Value : IVector3, IModel<Vector3Value>
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
        public void Reset()
        {
            X = ValueRules.FloatZero;
            Y = ValueRules.FloatZero;
            Z = ValueRules.FloatZero;
        }

        public VectorType GetModelType() => VectorType.Value;
        
        public object Clone() => Copy();
        IVector3 ICopyable<IVector3>.Copy() => new Vector3Value(X, Y, Z);
        public Vector3Value Copy() => new(X, Y, Z);
        
        public void Update(Vector3Value src)
        {
            X = src.X;
            Y = src.Y;
            Z = src.Z;
        }

        public void Pull(Vector3Value src)
        {
            X = src.X;
            Y = src.Y;
            Z = src.Z;
        }

        void IUpdatable<IVector3>.Update(IVector3 src)
        {
            if (src is Vector3Value value) Update(value);
        }
        void IMoveable<IVector3>.Pull(IVector3 src)
        {
            if (src is Vector3Value value) Pull(value);
        }

        public override bool Equals(object obj) => obj is Vector3Value value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
        
        public bool Equals(IVector3 other) => other is Vector3Value value && Equals(value);
        public bool Equals(Vector3Value other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = X.Equals(other.X)
                         && Y.Equals(other.Y)
                         && Z.Equals(other.Z);
            return result;
        }
    }
}