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
    /// A 2D vector rolled anywhere inside an axis-aligned rectangle. Each axis rolls independently -
    /// use Vector2Circle instead when the spread should be radial rather than boxy.
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(Vector2Rect.MinX), nameof(Vector2Rect.MaxX))]
    [RulePropertyOrder(nameof(Vector2Rect.MinY), nameof(Vector2Rect.MaxY))]
    public class Vector2Rect : IVector2, IModel<Vector2Rect>
    {
        // TODO add rule check for Min and Max, must be always Min < Max

        /// <summary> Left edge of the roll area. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MinX)]
        public float MinX { get; set; }

        /// <summary> Bottom edge of the roll area. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MinY)]
        public float MinY { get; set; }

        /// <summary> Right edge of the roll area. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MaxX)]
        public float MaxX { get; set; }

        /// <summary> Top edge of the roll area. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MaxY)]
        public float MaxY { get; set; }

        public Vector2Rect()
        {
            MinX = ValueRules.FloatZero;
            MinY = ValueRules.FloatZero;
            
            MaxX = ValueRules.FloatOne;
            MaxY = ValueRules.FloatOne;
        }
        public Vector2Rect(float minX, float minY, float maxX, float maxY)
        {
            MinX = minX;
            MinY = minY;
            
            MaxX = maxX;
            MaxY = maxY;
        }
        public void Reset()
        {
            MinX = ValueRules.FloatZero;
            MinY = ValueRules.FloatZero;
            
            MaxX = ValueRules.FloatOne;
            MaxY = ValueRules.FloatOne;
        }

        public VectorType GetModelType() => VectorType.RandomRect;

        public object Clone() => Copy();
        IVector2 ICopyable<IVector2>.Copy() => new Vector2Rect(MinX, MinY, MaxX, MaxY);
        public Vector2Rect Copy() => new(MinX, MinY, MaxX, MaxY);
        
        public void Update(Vector2Rect src)
        {
            MinX = src.MinX;
            MinY = src.MinY;
            MaxX = src.MaxX;
            MaxY = src.MaxY;
        }

        public void Pull(Vector2Rect src)
        {
            MinX = src.MinX;
            MinY = src.MinY;
            MaxX = src.MaxX;
            MaxY = src.MaxY;
        }

        void IUpdatable<IVector2>.Update(IVector2 src)
        {
            if (src is Vector2Rect value) Update(value);
        }
        void IMoveable<IVector2>.Pull(IVector2 src)
        {
            if (src is Vector2Rect value) Pull(value);
        }

        public override bool Equals(object obj) => obj is Vector2Rect value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(MinX, MinY, MaxX, MaxY);
        
        public bool Equals(IVector2 other) => other is Vector2Rect value && Equals(value);
        public bool Equals(Vector2Rect other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = MinX.Equals(other.MinX)
                         && MinY.Equals(other.MinY)
                         && MaxX.Equals(other.MaxX)
                         && MaxY.Equals(other.MaxY);
            return result;
        }
    }
}