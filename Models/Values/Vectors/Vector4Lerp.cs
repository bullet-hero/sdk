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
    /// A 4D vector rolled along the straight segment from From to To: ONE roll, read by
    /// every axis at the same position. Vector4Rect is the same two points read as a box,
    /// rolled per axis.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class Vector4Lerp : IVector4, IModel<Vector4Lerp>
    {
        /// <summary> The X the segment starts at. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MinX)]
        public float FromX { get; set; }

        /// <summary> The Y the segment starts at. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MinY)]
        public float FromY { get; set; }

        /// <summary> The Z the segment starts at. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MinZ)]
        public float FromZ { get; set; }

        /// <summary> The W the segment starts at. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MinW)]
        public float FromW { get; set; }

        /// <summary> The X the segment ends at. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MaxX)]
        public float ToX { get; set; }

        /// <summary> The Y the segment ends at. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MaxY)]
        public float ToY { get; set; }

        /// <summary> The Z the segment ends at. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MaxZ)]
        public float ToZ { get; set; }

        /// <summary> The W the segment ends at. </summary>
        [RuleInRange(ValueRules.MinFloatValue, ValueRules.MaxFloatValue)]
        [JsonProperty(Names.MaxW)]
        public float ToW { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public Vector4Lerp()
        {
            FromX = ValueRules.FloatZero;
            FromY = ValueRules.FloatZero;
            FromZ = ValueRules.FloatZero;
            FromW = ValueRules.FloatZero;

            ToX = ValueRules.FloatOne;
            ToY = ValueRules.FloatOne;
            ToZ = ValueRules.FloatOne;
            ToW = ValueRules.FloatOne;
        }

        /// <summary> Every member at once, in declaration order. </summary>
        public Vector4Lerp(float fromX, float fromY, float fromZ, float fromW, float toX, float toY, float toZ, float toW)
        {
            FromX = fromX;
            FromY = fromY;
            FromZ = fromZ;
            FromW = fromW;

            ToX = toX;
            ToY = toY;
            ToZ = toZ;
            ToW = toW;
        }

        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public VectorType GetModelType() => VectorType.RandomLerp;
    }
}
