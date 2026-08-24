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
    /// RGBA counterpart of Color3MinMax - alpha rolls independently too, which is what makes
    /// "random, sometimes barely visible" possible from a single value.
    /// </summary>
    [RuleContainer]
    [RulePropertyOrder(nameof(Color4MinMax.MinR), nameof(Color4MinMax.MaxR))]
    [RulePropertyOrder(nameof(Color4MinMax.MinG), nameof(Color4MinMax.MaxG))]
    [RulePropertyOrder(nameof(Color4MinMax.MinB), nameof(Color4MinMax.MaxB))]
    [RulePropertyOrder(nameof(Color4MinMax.MinA), nameof(Color4MinMax.MaxA))]
    public class Color4MinMax : IColor4, IModel<Color4MinMax>
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

        public Color4MinMax()
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
        public Color4MinMax(float minR, float minG, float minB, float minA, 
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
        public void Reset()
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

        public ColorType GetModelType() => ColorType.RandomMinMax;
        
        public object Clone() => Copy();
        IColor4 ICopyable<IColor4>.Copy() => new Color4MinMax(MinR, MinG, MinB, MinA, MaxR, MaxG, MaxB, MaxA);
        public Color4MinMax Copy() => new(MinR, MinG, MinB, MinA, MaxR, MaxG, MaxB, MaxA);

        public void Update(Color4MinMax src)
        {
            MinR = src.MinR;
            MinG = src.MinG;
            MinB = src.MinB;
            MinA = src.MinA;
            MaxR = src.MaxR;
            MaxG = src.MaxG;
            MaxB = src.MaxB;
            MaxA = src.MaxA;
        }

        public void Pull(Color4MinMax src)
        {
            MinR = src.MinR;
            MinG = src.MinG;
            MinB = src.MinB;
            MinA = src.MinA;
            MaxR = src.MaxR;
            MaxG = src.MaxG;
            MaxB = src.MaxB;
            MaxA = src.MaxA;
        }

        void IUpdatable<IColor4>.Update(IColor4 src)
        {
            if (src is Color4MinMax value) Update(value);
        }
        void IMoveable<IColor4>.Pull(IColor4 src)
        {
            if (src is Color4MinMax value) Pull(value);
        }

        public override bool Equals(object obj) => obj is Color4MinMax value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(MinR, MinG, MinB, MinA, MaxR, MaxG, MaxB, MaxA);
        
        public bool Equals(IColor4 other) => other is Color4MinMax value && Equals(value);
        public bool Equals(Color4MinMax other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = MinR.Equals(other.MinR)
                         && MinG.Equals(other.MinG)
                         && MinB.Equals(other.MinB)
                         && MinA.Equals(other.MinA)
                         && MaxR.Equals(other.MaxR)
                         && MaxG.Equals(other.MaxG)
                         && MaxB.Equals(other.MaxB)
                         && MaxA.Equals(other.MaxA);
            return result;
        }
    }
}