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
    /// A literal 2D vector - the workhorse of the format: positions, sizes, pivots and anchors all
    /// land here when the author picked exact numbers rather than a random variant.
    /// </summary>
    [RuleContainer]
    public class Vector2Value : IVector2, IModel<Vector2Value>
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
        public void Reset()
        {
            X = ValueRules.FloatZero;
            Y = ValueRules.FloatZero;
        }

        public VectorType GetModelType() => VectorType.Value;
        
        public object Clone() => Copy();
        IVector2 ICopyable<IVector2>.Copy() => new Vector2Value(X, Y);
        public Vector2Value Copy() => new(X, Y);
        
        public void Update(Vector2Value src)
        {
            X = src.X;
            Y = src.Y;
        }

        public void Pull(Vector2Value src)
        {
            X = src.X;
            Y = src.Y;
        }

        void IUpdatable<IVector2>.Update(IVector2 src)
        {
            if (src is Vector2Value value) Update(value);
        }
        void IMoveable<IVector2>.Pull(IVector2 src)
        {
            if (src is Vector2Value value) Pull(value);
        }

        public override bool Equals(object obj) => obj is Vector2Value value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        
        public bool Equals(IVector2 other) => other is Vector2Value value && Equals(value);
        public bool Equals(Vector2Value other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = X.Equals(other.X)
                         && Y.Equals(other.Y);
            return result;
        }
    }
}