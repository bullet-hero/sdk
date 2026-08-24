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
    /// A 4-component vector rolled inside a 4D box - every component drawn independently.
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(Vector4Rect.MinX), nameof(Vector4Rect.MaxX))]
    [RulePropertyOrder(nameof(Vector4Rect.MinY), nameof(Vector4Rect.MaxY))]
    [RulePropertyOrder(nameof(Vector4Rect.MinZ), nameof(Vector4Rect.MaxZ))]
    [RulePropertyOrder(nameof(Vector4Rect.MinW), nameof(Vector4Rect.MaxW))]
    public class Vector4Rect : IVector4, IModel<Vector4Rect>
    {
        /// <summary> Lower bound of the first component. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MinX)]
        public float MinX { get; set; }

        /// <summary> Lower bound of the second component. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MinY)]
        public float MinY { get; set; }

        /// <summary> Lower bound of the third component. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MinZ)]
        public float MinZ { get; set; }

        /// <summary> Lower bound of the fourth component. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MinW)]
        public float MinW { get; set; }

        /// <summary> Upper bound of the first component. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MaxX)]
        public float MaxX { get; set; }

        /// <summary> Upper bound of the second component. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MaxY)]
        public float MaxY { get; set; }

        /// <summary> Upper bound of the third component. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MaxZ)]
        public float MaxZ { get; set; }

        /// <summary> Upper bound of the fourth component. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MaxW)]
        public float MaxW { get; set; }

        public Vector4Rect()
        {
            MinX = ValueRules.FloatZero;
            MinY = ValueRules.FloatZero;
            MinZ = ValueRules.FloatZero;
            MinW = ValueRules.FloatZero;
            
            MaxX = ValueRules.FloatOne;
            MaxY = ValueRules.FloatOne;
            MaxZ = ValueRules.FloatOne;
            MaxW = ValueRules.FloatOne;
        }
        public Vector4Rect(float minX, float minY, float minZ, float minW, 
            float maxX, float maxY, float maxZ, float maxW)
        {
            MinX = minX;
            MinY = minY;
            MinZ = minZ;
            MinW = minW;
            
            MaxX = maxX;
            MaxY = maxY;
            MaxZ = maxZ;
            MaxW = maxW;
        }
        public void Reset()
        {
            MinX = ValueRules.FloatZero;
            MinY = ValueRules.FloatZero;
            MinZ = ValueRules.FloatZero;
            MinW = ValueRules.FloatZero;
            
            MaxX = ValueRules.FloatOne;
            MaxY = ValueRules.FloatOne;
            MaxZ = ValueRules.FloatOne;
            MaxW = ValueRules.FloatOne;
        }

        public VectorType GetModelType() => VectorType.RandomRect;

        public object Clone() => Copy();
        IVector4 ICopyable<IVector4>.Copy() => new Vector4Rect(MinX, MinY, MinZ, MinW, MaxX, MaxY, MaxZ, MaxW);
        public Vector4Rect Copy() => new(MinX, MinY, MinZ, MinW, MaxX, MaxY, MaxZ, MaxW);

        public void Update(Vector4Rect src)
        {
            MinX = src.MinX;
            MinY = src.MinY;
            MinZ = src.MinZ;
            MinW = src.MinW;
            MaxX = src.MaxX;
            MaxY = src.MaxY;
            MaxZ = src.MaxZ;
            MaxW = src.MaxW;
        }

        public void Pull(Vector4Rect src)
        {
            MinX = src.MinX;
            MinY = src.MinY;
            MinZ = src.MinZ;
            MinW = src.MinW;
            MaxX = src.MaxX;
            MaxY = src.MaxY;
            MaxZ = src.MaxZ;
            MaxW = src.MaxW;
        }

        void IUpdatable<IVector4>.Update(IVector4 src)
        {
            if (src is Vector4Rect value) Update(value);
        }
        void IMoveable<IVector4>.Pull(IVector4 src)
        {
            if (src is Vector4Rect value) Pull(value);
        }

        public override bool Equals(object obj) => obj is Vector4Rect value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(MinX, MinY, MinZ, MinW, MaxX, MaxY, MaxZ, MaxW);
        
        public bool Equals(IVector4 other) => other is Vector4Rect value && Equals(value);
        public bool Equals(Vector4Rect other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = MinX.Equals(other.MinX)
                         && MinY.Equals(other.MinY)
                         && MinZ.Equals(other.MinZ)
                         && MinW.Equals(other.MinW)
                         && MaxX.Equals(other.MaxX)
                         && MaxY.Equals(other.MaxY)
                         && MaxZ.Equals(other.MaxZ)
                         && MaxW.Equals(other.MaxW);
            return result;
        }
    }
}