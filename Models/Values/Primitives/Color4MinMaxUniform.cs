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
    /// An RGBA colour rolled with ONE roll shared by every channel, so the result is always a colour on
    /// the line between the two bounds. Color4MinMaxUniform is the same box rolled per channel.
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(Color4MinMaxUniform.MinR), nameof(Color4MinMaxUniform.MaxR))]
    [RulePropertyOrder(nameof(Color4MinMaxUniform.MinG), nameof(Color4MinMaxUniform.MaxG))]
    [RulePropertyOrder(nameof(Color4MinMaxUniform.MinB), nameof(Color4MinMaxUniform.MaxB))]
    [RulePropertyOrder(nameof(Color4MinMaxUniform.MinA), nameof(Color4MinMaxUniform.MaxA))]
    [GenerateModel]
    public sealed partial class Color4MinMaxUniform : IColor4, IModel<Color4MinMaxUniform>
    {
        /// <summary> Lower bound of the red roll. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.MinR)]
        public float MinR { get; set; }

        /// <summary> Lower bound of the green roll. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.MinG)]
        public float MinG { get; set; }

        /// <summary> Lower bound of the blue roll. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.MinB)]
        public float MinB { get; set; }

        /// <summary> Lower bound of the opacity roll. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.MinA)]
        public float MinA { get; set; }

        /// <summary> Upper bound of the red roll. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.MaxR)]
        public float MaxR { get; set; }

        /// <summary> Upper bound of the green roll. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.MaxG)]
        public float MaxG { get; set; }

        /// <summary> Upper bound of the blue roll. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.MaxB)]
        public float MaxB { get; set; }

        /// <summary> Upper bound of the opacity roll. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.MaxA)]
        public float MaxA { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public Color4MinMaxUniform()
        {
            MinR = ValueRules.MinColor;
            MinG = ValueRules.MinColor;
            MinB = ValueRules.MinColor;
            MinA = ValueRules.MinColor;
            
            MaxR = ValueRules.MaxColor;
            MaxG = ValueRules.MaxColor;
            MaxB = ValueRules.MaxColor;
            MaxA = ValueRules.MaxColor;
        }
        /// <summary> Every member at once, in declaration order. </summary>
        public Color4MinMaxUniform(float minR, float minG, float minB, float minA, 
            float maxR, float maxG, float maxB, float maxA)
        {
            MinR = minR;
            MinG = minG;
            MinB = minB;
            MinA = minA;
            
            MaxR = maxR;
            MaxG = maxG;
            MaxB = maxB;
            MaxA = maxA;
        }

        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public ColorType GetModelType() => ColorType.RandomMinMaxUniform;
    }
}