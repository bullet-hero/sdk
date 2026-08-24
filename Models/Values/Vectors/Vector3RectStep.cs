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
    /// Vector3Rect snapped to a cubic grid - random 3D placement constrained to cell centers.
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(Vector3RectStep.MinX), nameof(Vector3RectStep.MaxX))]
    [RulePropertyOrder(nameof(Vector3RectStep.MinY), nameof(Vector3RectStep.MaxY))]
    [RulePropertyOrder(nameof(Vector3RectStep.MinZ), nameof(Vector3RectStep.MaxZ))]
    public class Vector3RectStep : IVector3, IModel<Vector3RectStep>
    {
        /// <summary> Lower X bound, and the X origin the grid is measured from. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MinX)]
        public float MinX { get; set; }

        /// <summary> Lower Y bound, and the Y origin the grid is measured from. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MinY)]
        public float MinY { get; set; }

        /// <summary> Lower Z bound, and the Z origin the grid is measured from. </summary>
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

        /// <summary> Cell size, shared by all three axes. </summary>
        [RuleInRange(ValueRules.FloatZero, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.Step)]
        public float Step { get; set; }

        public Vector3RectStep()
        {
            MinX = ValueRules.FloatZero;
            MinY = ValueRules.FloatZero;
            MinZ = ValueRules.FloatZero;
            
            MaxX = ValueRules.FloatOne;
            MaxY = ValueRules.FloatOne;
            MaxZ = ValueRules.FloatOne;
            
            Step = ValueRules.FloatOne;
        }
        public Vector3RectStep(float minX, float minY, float minZ, float maxX, float maxY, float maxZ, float step)
        {
            MinX = minX;
            MinY = minY;
            MinZ = minZ;
            
            MaxX = maxX;
            MaxY = maxY;
            MaxZ = maxZ;
            
            Step = step;
        }
        public void Reset()
        {
            MinX = ValueRules.FloatZero;
            MinY = ValueRules.FloatZero;
            MinZ = ValueRules.FloatZero;
            
            MaxX = ValueRules.FloatOne;
            MaxY = ValueRules.FloatOne;
            MaxZ = ValueRules.FloatOne;
            
            Step = ValueRules.FloatOne;
        }

        public VectorType GetModelType() => VectorType.RandomRectStep;

        public object Clone() => Copy();
        IVector3 ICopyable<IVector3>.Copy() => new Vector3RectStep(MinX, MinY, MinZ, MaxX, MaxY, MaxZ, Step);
        public Vector3RectStep Copy() => new(MinX, MinY, MinZ, MaxX, MaxY, MaxZ, Step);

        public void Update(Vector3RectStep src)
        {
            MinX = src.MinX;
            MinY = src.MinY;
            MinZ = src.MinZ;
            MaxX = src.MaxX;
            MaxY = src.MaxY;
            MaxZ = src.MaxZ;
            Step = src.Step;
        }

        public void Pull(Vector3RectStep src)
        {
            MinX = src.MinX;
            MinY = src.MinY;
            MinZ = src.MinZ;
            MaxX = src.MaxX;
            MaxY = src.MaxY;
            MaxZ = src.MaxZ;
            Step = src.Step;
        }

        void IUpdatable<IVector3>.Update(IVector3 src)
        {
            if (src is Vector3RectStep value) Update(value);
        }
        void IMoveable<IVector3>.Pull(IVector3 src)
        {
            if (src is Vector3RectStep value) Pull(value);
        }

        public override bool Equals(object obj) => obj is Vector3RectStep value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(MinX, MinY, MinZ, MaxX, MaxY, MaxZ, Step);
        
        public bool Equals(IVector3 other) => other is Vector3RectStep value && Equals(value);
        public bool Equals(Vector3RectStep other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = MinX.Equals(other.MinX)
                         && MinY.Equals(other.MinY)
                         && MinZ.Equals(other.MinZ)
                         && MaxX.Equals(other.MaxX)
                         && MaxY.Equals(other.MaxY)
                         && MaxZ.Equals(other.MaxZ)
                         && Step.Equals(other.Step);
            return result;
        }
    }
}