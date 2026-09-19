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
    /// An RGB colour rolled with ONE roll shared by every channel, so the result is always a colour on
    /// the line between the two bounds. Color3MinMaxUniform is the same box rolled per channel.
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(Color3MinMaxUniform.MinR), nameof(Color3MinMaxUniform.MaxR))]
    [RulePropertyOrder(nameof(Color3MinMaxUniform.MinG), nameof(Color3MinMaxUniform.MaxG))]
    [RulePropertyOrder(nameof(Color3MinMaxUniform.MinB), nameof(Color3MinMaxUniform.MaxB))]
    [GenerateModel]
    public sealed partial class Color3MinMaxUniform : IColor3, IModel<Color3MinMaxUniform>
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

        /// <summary> Upper bound of the red roll. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.MaxR)]
        public float MaxR { get; set; }

        /// <summary> Upper bound of the green roll. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.MaxG)]
        public float MaxG { get; set; }

        /// <summary> Upper bound of the blue roll. Channels roll independently, so the result is any
        /// color in the box, not a point on the Min-Max line. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.MaxB)]
        public float MaxB { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public Color3MinMaxUniform()
        {
            MinR = ValueRules.MinColor;
            MinG = ValueRules.MinColor;
            MinB = ValueRules.MinColor;

            MaxR = ValueRules.MaxColor;
            MaxG = ValueRules.MaxColor;
            MaxB = ValueRules.MaxColor;
        }
        /// <summary> Every member at once, in declaration order. </summary>
        public Color3MinMaxUniform(float minR, float minG, float minB,
            float maxR, float maxG, float maxB)
        {
            MinR = minR;
            MinG = minG;
            MinB = minB;

            MaxR = maxR;
            MaxG = maxG;
            MaxB = maxB;
        }

        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public ColorType GetModelType() => ColorType.RandomMinMaxUniform;
    }
}
