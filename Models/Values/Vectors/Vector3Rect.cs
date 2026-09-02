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
    /// A 3D vector rolled inside an axis-aligned box - Vector2Rect with a depth axis added.
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(Vector3Rect.MinX), nameof(Vector3Rect.MaxX))]
    [RulePropertyOrder(nameof(Vector3Rect.MinY), nameof(Vector3Rect.MaxY))]
    [RulePropertyOrder(nameof(Vector3Rect.MinZ), nameof(Vector3Rect.MaxZ))]
    [GenerateModel]
    public sealed partial class Vector3Rect : IVector3, IModel<Vector3Rect>
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

        public VectorType GetModelType() => VectorType.RandomRect;
    }
}