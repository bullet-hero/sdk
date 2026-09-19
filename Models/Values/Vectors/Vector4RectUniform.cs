using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Values
{
    /// <summary>
    /// A 4D vector rolled inside a box with ONE roll shared by all four axes, so the point slides
    /// along the box's diagonal. Vector4RectUniform is the same box rolled per axis.
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(Vector4RectUniform.MinX), nameof(Vector4RectUniform.MaxX))]
    [RulePropertyOrder(nameof(Vector4RectUniform.MinY), nameof(Vector4RectUniform.MaxY))]
    [RulePropertyOrder(nameof(Vector4RectUniform.MinZ), nameof(Vector4RectUniform.MaxZ))]
    [RulePropertyOrder(nameof(Vector4RectUniform.MinW), nameof(Vector4RectUniform.MaxW))]
    [GenerateModel]
    public sealed partial class Vector4RectUniform : IVector4, IModel<Vector4RectUniform>
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

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public Vector4RectUniform()
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
        /// <summary> Every member at once, in declaration order. </summary>
        public Vector4RectUniform(float minX, float minY, float minZ, float minW, 
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

        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public VectorType GetModelType() => VectorType.RandomRectUniform;
    }
}