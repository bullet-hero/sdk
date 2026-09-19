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
    /// A 3D vector rolled inside a box with ONE roll shared by all three axes, so the point slides
    /// along the box's diagonal. Vector3RectUniform is the same box rolled per axis.
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(Vector3RectUniform.MinX), nameof(Vector3RectUniform.MaxX))]
    [RulePropertyOrder(nameof(Vector3RectUniform.MinY), nameof(Vector3RectUniform.MaxY))]
    [RulePropertyOrder(nameof(Vector3RectUniform.MinZ), nameof(Vector3RectUniform.MaxZ))]
    [GenerateModel]
    public sealed partial class Vector3RectUniform : IVector3, IModel<Vector3RectUniform>
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

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public Vector3RectUniform()
        {
            MinX = ValueRules.FloatZero;
            MinY = ValueRules.FloatZero;
            MinZ = ValueRules.FloatZero;
            
            MaxX = ValueRules.FloatOne;
            MaxY = ValueRules.FloatOne;
            MaxZ = ValueRules.FloatOne;
        }
        /// <summary> Every member at once, in declaration order. </summary>
        public Vector3RectUniform(float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
        {
            MinX = minX;
            MinY = minY;
            MinZ = minZ;
            
            MaxX = maxX;
            MaxY = maxY;
            MaxZ = maxZ;
        }

        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public VectorType GetModelType() => VectorType.RandomRectUniform;
    }
}