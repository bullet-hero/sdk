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
    /// Vector3Rect snapped to a cubic grid - random 3D placement constrained to cell centers.
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(Vector3RectStep.MinX), nameof(Vector3RectStep.MaxX))]
    [RulePropertyOrder(nameof(Vector3RectStep.MinY), nameof(Vector3RectStep.MaxY))]
    [RulePropertyOrder(nameof(Vector3RectStep.MinZ), nameof(Vector3RectStep.MaxZ))]
    [GenerateModel]
    public sealed partial class Vector3RectStep : IVector3, IModel<Vector3RectStep>
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

        public VectorType GetModelType() => VectorType.RandomRectStep;
    }
}