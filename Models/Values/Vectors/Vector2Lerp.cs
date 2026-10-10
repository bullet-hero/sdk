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
    // A segment, not a box: ONE roll is read by every component, so the value never leaves the line From-To.
    // No RulePropertyOrder, deliberately: ordering the pair would swap From and To and make a line that
    // falls as it advances (x up, y down) unrepresentable. Models/Values/CLAUDE.md

    /// <summary>
    /// A 2D vector rolled along the segment From-To: ONE roll read by every axis.
    /// Vector2Rect is the same two points read as a box, rolled per axis.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class Vector2Lerp : IVector2, IModel<Vector2Lerp>
    {
        /// <summary> The X the segment starts at. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MinX)]
        public float FromX { get; set; }

        /// <summary> The Y the segment starts at. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MinY)]
        public float FromY { get; set; }

        /// <summary> The X the segment ends at. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MaxX)]
        public float ToX { get; set; }

        /// <summary> The Y the segment ends at. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MaxY)]
        public float ToY { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public Vector2Lerp()
        {
            FromX = ValueRules.FloatZero;
            FromY = ValueRules.FloatZero;

            ToX = ValueRules.FloatOne;
            ToY = ValueRules.FloatOne;
        }

        /// <summary> Every member at once, in declaration order. </summary>
        public Vector2Lerp(float fromX, float fromY, float toX, float toY)
        {
            FromX = fromX;
            FromY = fromY;

            ToX = toX;
            ToY = toY;
        }

        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public VectorType GetModelType() => VectorType.RandomLerp;
    }
}
