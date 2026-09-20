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
    // A segment, not a box, and that is the whole difference from its Rect sibling. One roll picks
    // a position along it and every component is read at that same position, so the components are
    // perfectly correlated: the value travels the straight line from `From` to `To` and never
    // leaves it. Pinning one axis is therefore a degenerate range on that axis alone - give it the
    // same number in both ends and it stops moving while the rest still roll. NO RulePropertyOrder
    // here, deliberately: a segment has no smaller end, and ordering the pair would swap `From` and
    // `To` on the way in, making a line that falls as it advances (x up, y down) unrepresentable -
    // which is the case correlation exists for.

    /// <summary>
    /// Vector2Lerp snapped to a grid: the shared roll picks the point on the segment,
    /// then each axis is quantized to its own grid, so one step means the same distance
    /// on all of them.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class Vector2LerpStep : IVector2, IModel<Vector2LerpStep>
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

        /// <summary> Cell size, shared by every axis - one square grid, not per-axis spacing. </summary>
        [RuleInRange(ValueRules.FloatZero, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.Step)]
        public float Step { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public Vector2LerpStep()
        {
            FromX = ValueRules.FloatZero;
            FromY = ValueRules.FloatZero;

            ToX = ValueRules.FloatOne;
            ToY = ValueRules.FloatOne;

            Step = ValueRules.FloatOne;
        }

        /// <summary> Every member at once, in declaration order. </summary>
        public Vector2LerpStep(float fromX, float fromY, float toX, float toY, float step)
        {
            FromX = fromX;
            FromY = fromY;

            ToX = toX;
            ToY = toY;

            Step = step;
        }

        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public VectorType GetModelType() => VectorType.RandomLerpStep;
    }
}
