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
    /// Vector4Rect quantized to a grid - the 4D end of the Value/Rect/RectStep/Circle family that
    /// every IVector interface repeats identically.
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(Vector4RectStep.MinX), nameof(Vector4RectStep.MaxX))]
    [RulePropertyOrder(nameof(Vector4RectStep.MinY), nameof(Vector4RectStep.MaxY))]
    [RulePropertyOrder(nameof(Vector4RectStep.MinZ), nameof(Vector4RectStep.MaxZ))]
    [RulePropertyOrder(nameof(Vector4RectStep.MinW), nameof(Vector4RectStep.MaxW))]
    public class Vector4RectStep : IVector4, IModel<Vector4RectStep>
    {
        /// <summary> Lower bound of the first component, and its grid origin. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MinX)]
        public float MinX { get; set; }

        /// <summary> Lower bound of the second component, and its grid origin. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MinY)]
        public float MinY { get; set; }

        /// <summary> Lower bound of the third component, and its grid origin. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MinZ)]
        public float MinZ { get; set; }

        /// <summary> Lower bound of the fourth component, and its grid origin. </summary>
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

        /// <summary> Cell size, shared by all four components. </summary>
        [RuleInRange(ValueRules.FloatZero, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.Step)]
        public float Step { get; set; }

        public Vector4RectStep()
        {
            MinX = ValueRules.FloatZero;
            MinY = ValueRules.FloatZero;
            MinZ = ValueRules.FloatZero;
            MinW = ValueRules.FloatZero;
            
            MaxX = ValueRules.FloatOne;
            MaxY = ValueRules.FloatOne;
            MaxZ = ValueRules.FloatOne;
            MaxW = ValueRules.FloatOne;
            
            Step = ValueRules.FloatOne;
        }
        public Vector4RectStep(float minX, float minY, float minZ, float minW, 
            float maxX, float maxY, float maxZ, float maxW, float step)
        {
            MinX = minX;
            MinY = minY;
            MinZ = minZ;
            MinW = minW;
            
            MaxX = maxX;
            MaxY = maxY;
            MaxZ = maxZ;
            MaxW = maxW;
            
            Step = step;
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
            
            Step = ValueRules.FloatOne;
        }

        public VectorType GetModelType() => VectorType.RandomRectStep;

        public object Clone() => Copy();
        IVector4 ICopyable<IVector4>.Copy() => new Vector4RectStep(MinX, MinY, MinZ, MinW, MaxX, MaxY, MaxZ, MaxW, Step);
        public Vector4RectStep Copy() => new(MinX, MinY, MinZ, MinW, MaxX, MaxY, MaxZ, MaxW, Step);

        public void Update(Vector4RectStep src)
        {
            MinX = src.MinX;
            MinY = src.MinY;
            MinZ = src.MinZ;
            MinW = src.MinW;
            MaxX = src.MaxX;
            MaxY = src.MaxY;
            MaxZ = src.MaxZ;
            MaxW = src.MaxW;
            Step = src.Step;
        }

        public void Pull(Vector4RectStep src)
        {
            MinX = src.MinX;
            MinY = src.MinY;
            MinZ = src.MinZ;
            MinW = src.MinW;
            MaxX = src.MaxX;
            MaxY = src.MaxY;
            MaxZ = src.MaxZ;
            MaxW = src.MaxW;
            Step = src.Step;
        }

        void IUpdatable<IVector4>.Update(IVector4 src)
        {
            if (src is Vector4RectStep value) Update(value);
        }
        void IMoveable<IVector4>.Pull(IVector4 src)
        {
            if (src is Vector4RectStep value) Pull(value);
        }

        public override bool Equals(object obj) => obj is Vector4RectStep value && Equals(value);
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(MinX);
            hashCode.Add(MinY);
            hashCode.Add(MinZ);
            hashCode.Add(MinW);
            hashCode.Add(MaxX);
            hashCode.Add(MaxY);
            hashCode.Add(MaxZ);
            hashCode.Add(MaxW);
            hashCode.Add(Step);
            return hashCode.ToHashCode();
        }
        
        public bool Equals(IVector4 other) => other is Vector4RectStep value && Equals(value);
        public bool Equals(Vector4RectStep other)
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
                         && MaxW.Equals(other.MaxW)
                         && Step.Equals(other.Step);
            return result;
        }
    }
}