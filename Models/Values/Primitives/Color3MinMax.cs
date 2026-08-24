using System;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Values
{
    /// <summary>
    /// An RGB color rolled per channel inside a box of colors. The tag survives serialization, so the
    /// player re-rolls it every frame instead of freezing one color at load time.
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(Color3MinMax.MinR), nameof(Color3MinMax.MaxR))]
    [RulePropertyOrder(nameof(Color3MinMax.MinG), nameof(Color3MinMax.MaxG))]
    [RulePropertyOrder(nameof(Color3MinMax.MinB), nameof(Color3MinMax.MaxB))]
    public class Color3MinMax : IColor3, IModel<Color3MinMax>
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

        public Color3MinMax()
        {
            MinR = ValueRules.MinColor;
            MinG = ValueRules.MinColor;
            MinB = ValueRules.MinColor;

            MaxR = ValueRules.MaxColor;
            MaxG = ValueRules.MaxColor;
            MaxB = ValueRules.MaxColor;
        }
        public Color3MinMax(float minR, float minG, float minB,
            float maxR, float maxG, float maxB)
        {
            MinR = minR;
            MinG = minG;
            MinB = minB;

            MaxR = maxR;
            MaxG = maxG;
            MaxB = maxB;
        }
        public void Reset()
        {
            MinR = ValueRules.MinColor;
            MinG = ValueRules.MinColor;
            MinB = ValueRules.MinColor;

            MaxR = ValueRules.MaxColor;
            MaxG = ValueRules.MaxColor;
            MaxB = ValueRules.MaxColor;
        }

        public ColorType GetModelType() => ColorType.RandomMinMax;

        public object Clone() => Copy();
        IColor3 ICopyable<IColor3>.Copy() => new Color3MinMax(MinR, MinG, MinB, MaxR, MaxG, MaxB);
        public Color3MinMax Copy() => new(MinR, MinG, MinB, MaxR, MaxG, MaxB);

        public void Update(Color3MinMax src)
        {
            MinR = src.MinR;
            MinG = src.MinG;
            MinB = src.MinB;
            MaxR = src.MaxR;
            MaxG = src.MaxG;
            MaxB = src.MaxB;
        }

        public void Pull(Color3MinMax src)
        {
            MinR = src.MinR;
            MinG = src.MinG;
            MinB = src.MinB;
            MaxR = src.MaxR;
            MaxG = src.MaxG;
            MaxB = src.MaxB;
        }

        void IUpdatable<IColor3>.Update(IColor3 src)
        {
            if (src is Color3MinMax value) Update(value);
        }
        void IMoveable<IColor3>.Pull(IColor3 src)
        {
            if (src is Color3MinMax value) Pull(value);
        }

        public override bool Equals(object obj) => obj is Color3MinMax value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(MinR, MinG, MinB, MaxR, MaxG, MaxB);

        public bool Equals(IColor3 other) => other is Color3MinMax value && Equals(value);
        public bool Equals(Color3MinMax other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = MinR.Equals(other.MinR)
                         && MinG.Equals(other.MinG)
                         && MinB.Equals(other.MinB)
                         && MaxR.Equals(other.MaxR)
                         && MaxG.Equals(other.MaxG)
                         && MaxB.Equals(other.MaxB);
            return result;
        }
    }
}
