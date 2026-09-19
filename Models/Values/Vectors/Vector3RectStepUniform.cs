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
    /// Vector3RectUniform snapped to a grid: the shared roll decides the point on the diagonal, then
    /// each axis is quantized to its own grid.
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(Vector3RectStepUniform.MinX), nameof(Vector3RectStepUniform.MaxX))]
    [RulePropertyOrder(nameof(Vector3RectStepUniform.MinY), nameof(Vector3RectStepUniform.MaxY))]
    [RulePropertyOrder(nameof(Vector3RectStepUniform.MinZ), nameof(Vector3RectStepUniform.MaxZ))]
    [GenerateModel]
    public sealed partial class Vector3RectStepUniform : IVector3, IModel<Vector3RectStepUniform>
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

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public Vector3RectStepUniform()
        {
            MinX = ValueRules.FloatZero;
            MinY = ValueRules.FloatZero;
            MinZ = ValueRules.FloatZero;
            
            MaxX = ValueRules.FloatOne;
            MaxY = ValueRules.FloatOne;
            MaxZ = ValueRules.FloatOne;
            
            Step = ValueRules.FloatOne;
        }
        /// <summary> Every member at once, in declaration order. </summary>
        public Vector3RectStepUniform(float minX, float minY, float minZ, float maxX, float maxY, float maxZ, float step)
        {
            MinX = minX;
            MinY = minY;
            MinZ = minZ;
            
            MaxX = maxX;
            MaxY = maxY;
            MaxZ = maxZ;
            
            Step = step;
        }

        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public VectorType GetModelType() => VectorType.RandomRectStepUniform;
    }
}