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
    /// A 3D vector rolled inside an axis-aligned box - Vector2Rect with a depth axis added.
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(Vector3Rect.MinX), nameof(Vector3Rect.MaxX))]
    [RulePropertyOrder(nameof(Vector3Rect.MinY), nameof(Vector3Rect.MaxY))]
    [RulePropertyOrder(nameof(Vector3Rect.MinZ), nameof(Vector3Rect.MaxZ))]
    public class Vector3Rect : IVector3, IModel<Vector3Rect>
    {
        /// <summary> Lower X bound of the roll box. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MinX)]
        public float MinX { get; set; }

        /// <summary> Lower Y bound of the roll box. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MinY)]
        public float MinY { get; set; }

        /// <summary> Lower Z bound of the roll box. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MinZ)]
        public float MinZ { get; set; }

        /// <summary> Upper X bound of the roll box. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MaxX)]
        public float MaxX { get; set; }

        /// <summary> Upper Y bound of the roll box. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MaxY)]
        public float MaxY { get; set; }

        /// <summary> Upper Z bound of the roll box. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MaxZ)]
        public float MaxZ { get; set; }

        public Vector3Rect()
        {
            MinX = ValueRules.FloatZero;
            MinY = ValueRules.FloatZero;
            MinZ = ValueRules.FloatZero;
            
            MaxX = ValueRules.FloatOne;
            MaxY = ValueRules.FloatOne;
            MaxZ = ValueRules.FloatOne;
        }
        public Vector3Rect(float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
        {
            MinX = minX;
            MinY = minY;
            MinZ = minZ;
            
            MaxX = maxX;
            MaxY = maxY;
            MaxZ = maxZ;
        }
        public void Reset()
        {
            MinX = ValueRules.FloatZero;
            MinY = ValueRules.FloatZero;
            MinZ = ValueRules.FloatZero;
            
            MaxX = ValueRules.FloatOne;
            MaxY = ValueRules.FloatOne;
            MaxZ = ValueRules.FloatOne;
        }

        public VectorType GetModelType() => VectorType.RandomRect;

        public object Clone() => Copy();
        IVector3 ICopyable<IVector3>.Copy() => new Vector3Rect(MinX, MinY, MinZ, MaxX, MaxY, MaxZ);
        public Vector3Rect Copy() => new(MinX, MinY, MinZ, MaxX, MaxY, MaxZ);

        public void Update(Vector3Rect src)
        {
            MinX = src.MinX;
            MinY = src.MinY;
            MinZ = src.MinZ;
            MaxX = src.MaxX;
            MaxY = src.MaxY;
            MaxZ = src.MaxZ;
        }

        public void Pull(Vector3Rect src)
        {
            MinX = src.MinX;
            MinY = src.MinY;
            MinZ = src.MinZ;
            MaxX = src.MaxX;
            MaxY = src.MaxY;
            MaxZ = src.MaxZ;
        }

        void IUpdatable<IVector3>.Update(IVector3 src)
        {
            if (src is Vector3Rect value) Update(value);
        }
        void IMoveable<IVector3>.Pull(IVector3 src)
        {
            if (src is Vector3Rect value) Pull(value);
        }

        public override bool Equals(object obj) => obj is Vector3Rect value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(MinX, MinY, MinZ, MaxX, MaxY, MaxZ);
        
        public bool Equals(IVector3 other) => other is Vector3Rect value && Equals(value);
        public bool Equals(Vector3Rect other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = MinX.Equals(other.MinX)
                         && MinY.Equals(other.MinY)
                         && MinZ.Equals(other.MinZ)
                         && MaxX.Equals(other.MaxX)
                         && MaxY.Equals(other.MaxY)
                         && MaxZ.Equals(other.MaxZ);
            return result;
        }
    }
}