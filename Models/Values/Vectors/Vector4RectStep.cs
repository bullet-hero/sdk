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
    /// Vector4Rect quantized to a grid - the 4D end of the Value/Rect/RectStep/Circle family that
    /// every IVector interface repeats identically.
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(Vector4RectStep.MinX), nameof(Vector4RectStep.MaxX))]
    [RulePropertyOrder(nameof(Vector4RectStep.MinY), nameof(Vector4RectStep.MaxY))]
    [RulePropertyOrder(nameof(Vector4RectStep.MinZ), nameof(Vector4RectStep.MaxZ))]
    [RulePropertyOrder(nameof(Vector4RectStep.MinW), nameof(Vector4RectStep.MaxW))]
    [GenerateModel]
    public sealed partial class Vector4RectStep : IVector4, IModel<Vector4RectStep>
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

        public VectorType GetModelType() => VectorType.RandomRectStep;
    }
}