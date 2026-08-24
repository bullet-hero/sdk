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
    /// A 2D vector rolled inside a disc around a center. Direction-neutral, unlike Vector2Rect whose
    /// corners bias the spread diagonally.
    /// </summary>
    [RuleContainer]
    public class Vector2Circle : IVector2, IModel<Vector2Circle>
    {
        /// <summary> Center X the disc is built around. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.CoordX)]
        public float X { get; set; }

        /// <summary> Center Y the disc is built around. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.CoordY)]
        public float Y { get; set; }

        /// <summary> Maximum distance from the center. Zero collapses the value to the center point. </summary>
        [RuleInRange(ValueRules.FloatZero, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.Radius)]
        public float Radius { get; set; }
        
        public Vector2Circle()
        {
            X = ValueRules.FloatZero;
            Y = ValueRules.FloatZero;
            Radius = ValueRules.FloatOne;
        }
        public Vector2Circle(float x, float y, float radius)
        {
            X = x;
            Y = y;
            Radius = radius;
        }
        public void Reset()
        {
            X = ValueRules.FloatZero;
            Y = ValueRules.FloatZero;
            Radius = ValueRules.FloatOne;
        }

        public VectorType GetModelType() => VectorType.RandomCircle;
        
        public object Clone() => Copy();
        IVector2 ICopyable<IVector2>.Copy() => new Vector2Circle(X, Y, Radius);
        public Vector2Circle Copy() => new(X, Y, Radius);

        public void Update(Vector2Circle src)
        {
            X = src.X;
            Y = src.Y;
            Radius = src.Radius;
        }

        public void Pull(Vector2Circle src)
        {
            X = src.X;
            Y = src.Y;
            Radius = src.Radius;
        }

        void IUpdatable<IVector2>.Update(IVector2 src)
        {
            if (src is Vector2Circle value) Update(value);
        }
        void IMoveable<IVector2>.Pull(IVector2 src)
        {
            if (src is Vector2Circle value) Pull(value);
        }

        public override bool Equals(object obj) => obj is Vector2Circle value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(X, Y, Radius);
        
        public bool Equals(IVector2 other) => other is Vector2Circle value && Equals(value);
        public bool Equals(Vector2Circle other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = X.Equals(other.X)
                         && Y.Equals(other.Y)
                         && Radius.Equals(other.Radius);
            return result;
        }
    }
}