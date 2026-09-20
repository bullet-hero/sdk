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
    /// An RGB colour rolled along the straight segment between two colours: ONE
    /// roll, read by every channel at the same position, so the result is always a colour on
    /// that line. Color3MinMax is the same two colours read as a box, rolled per channel.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class Color3Lerp : IColor3, IModel<Color3Lerp>
    {
        /// <summary> The R the segment starts at. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.MinR)]
        public float FromR { get; set; }

        /// <summary> The G the segment starts at. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.MinG)]
        public float FromG { get; set; }

        /// <summary> The B the segment starts at. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.MinB)]
        public float FromB { get; set; }

        /// <summary> The R the segment ends at. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.MaxR)]
        public float ToR { get; set; }

        /// <summary> The G the segment ends at. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.MaxG)]
        public float ToG { get; set; }

        /// <summary> The B the segment ends at. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.MaxB)]
        public float ToB { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public Color3Lerp()
        {
            FromR = ValueRules.MinColor;
            FromG = ValueRules.MinColor;
            FromB = ValueRules.MinColor;

            ToR = ValueRules.MaxColor;
            ToG = ValueRules.MaxColor;
            ToB = ValueRules.MaxColor;
        }

        /// <summary> Every member at once, in declaration order. </summary>
        public Color3Lerp(float fromR, float fromG, float fromB, float toR, float toG, float toB)
        {
            FromR = fromR;
            FromG = fromG;
            FromB = fromB;

            ToR = toR;
            ToG = toG;
            ToB = toB;
        }

        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public ColorType GetModelType() => ColorType.RandomLerp;
    }
}
