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
    /// A 2D vector rolled inside an axis-aligned rectangle with ONE roll shared by both axes, so the
    /// point slides along the rectangle's diagonal. Vector2RectUniform is the same rectangle rolled per axis.
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(Vector2RectUniform.MinX), nameof(Vector2RectUniform.MaxX))]
    [RulePropertyOrder(nameof(Vector2RectUniform.MinY), nameof(Vector2RectUniform.MaxY))]
    [GenerateModel]
    public sealed partial class Vector2RectUniform : IVector2, IModel<Vector2RectUniform>
    {
        /// <summary> Left edge of the roll area. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MinX)]
        public float MinX { get; set; }

        /// <summary> Bottom edge of the roll area. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MinY)]
        public float MinY { get; set; }

        /// <summary> Right edge of the roll area. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MaxX)]
        public float MaxX { get; set; }

        /// <summary> Top edge of the roll area. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MaxY)]
        public float MaxY { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public Vector2RectUniform()
        {
            MinX = ValueRules.FloatZero;
            MinY = ValueRules.FloatZero;

            MaxX = ValueRules.FloatOne;
            MaxY = ValueRules.FloatOne;
        }

        /// <summary> Built from its X, Y, X and Y. </summary>
        public Vector2RectUniform(float minX, float minY, float maxX, float maxY)
        {
            MinX = minX;
            MinY = minY;

            MaxX = maxX;
            MaxY = maxY;
        }

        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public VectorType GetModelType() => VectorType.RandomRectUniform;
    }
}