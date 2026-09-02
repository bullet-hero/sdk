using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming

namespace BH.SDK.Models.Values
{
    // Alpha is not stored: any IColor3 is implicitly fully opaque (alpha = 1).

    /// <summary>
    /// A literal RGB color - the plain, non-random, non-themed IColor3 variant. What "I picked this
    /// exact color" serializes to; the other two variants defer the decision to a theme or a dice roll.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class Color3Value : IColor3, IModel<Color3Value>
    {
        /// <summary> Red channel, normalized 0..1 (not 0..255). </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.ChannelR)]
        public float R { get; set; }

        /// <summary> Green channel, normalized 0..1. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.ChannelG)]
        public float G { get; set; }

        /// <summary> Blue channel, normalized 0..1. </summary>
        [RuleInRange(ValueRules.MinColor, ValueRules.MaxColor)]
        [JsonProperty(Names.ChannelB)]
        public float B { get; set; }

        public Color3Value()
        {
            R = ValueRules.MaxColor;
            G = ValueRules.MaxColor;
            B = ValueRules.MaxColor;
        }
        public Color3Value(float r, float g, float b)
        {
            R = r;
            G = g;
            B = b;
        }

        public ColorType GetModelType() => ColorType.Value;

        public static Color3Value white => new(1f, 1f, 1f);
        public static Color3Value black => new(0f, 0f, 0f);
    }
}
