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
    /// A 4-component vector rolled inside a 4D box - every component drawn independently.
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(Vector4Rect.MinX), nameof(Vector4Rect.MaxX))]
    [RulePropertyOrder(nameof(Vector4Rect.MinY), nameof(Vector4Rect.MaxY))]
    [RulePropertyOrder(nameof(Vector4Rect.MinZ), nameof(Vector4Rect.MaxZ))]
    [RulePropertyOrder(nameof(Vector4Rect.MinW), nameof(Vector4Rect.MaxW))]
    [GenerateModel]
    public sealed partial class Vector4Rect : IVector4, IModel<Vector4Rect>
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

        public VectorType GetModelType() => VectorType.RandomRect;
    }
}