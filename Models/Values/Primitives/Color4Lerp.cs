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
    /// An RGBA colour rolled along the segment between two colours: ONE roll read by every channel.
    /// Color4MinMax is the same two colours read as a box, rolled per channel.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class Color4Lerp : IColor4, IModel<Color4Lerp>
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

        /// <summary> The A the segment starts at. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.MinA)]
        public float FromA { get; set; }

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

        /// <summary> The A the segment ends at. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.MaxA)]
        public float ToA { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public Color4Lerp()
        {
            FromR = ValueRules.MinColor;
            FromG = ValueRules.MinColor;
            FromB = ValueRules.MinColor;
            FromA = ValueRules.MinColor;

            ToR = ValueRules.MaxColor;
            ToG = ValueRules.MaxColor;
            ToB = ValueRules.MaxColor;
            ToA = ValueRules.MaxColor;
        }

        /// <summary> Every member at once, in declaration order. </summary>
        public Color4Lerp(float fromR, float fromG, float fromB, float fromA, float toR, float toG, float toB, float toA)
        {
            FromR = fromR;
            FromG = fromG;
            FromB = fromB;
            FromA = fromA;

            ToR = toR;
            ToG = toG;
            ToB = toB;
            ToA = toA;
        }

        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public ColorType GetModelType() => ColorType.RandomLerp;
    }
}
