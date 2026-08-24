using System;
using BH.SDK.Models.Enums.Effects;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Effects;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Effects
{
    /// <summary>
    /// Each particle picks one random spot on the gradient and keeps that color - a palette to draw
    /// from, not an animation. The gradient is a color set here, unlike the OverLife/BySpeed variants
    /// where it is a curve in time or speed.
    /// </summary>
    [RuleContainer]
    public class EffectColorGradientRandom : IEffectColor, IModel<EffectColorGradientRandom>
    {
        /// <summary> Ramp the per-particle color is drawn from. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Gradient)]
        public GradientValue Gradient { get; set; }

        public EffectColorType GetModelType() => EffectColorType.GradientRandom;

        public EffectColorGradientRandom()
        {
            Gradient = EffectRules.GetGradient_Default();
        }
        public EffectColorGradientRandom(GradientValue gradient)
        {
            Gradient = gradient;
        }
        public void Reset()
        {
            Gradient = EffectRules.GetGradient_Default();
        }

        public object Clone() => Copy();
        IEffectColor ICopyable<IEffectColor>.Copy() => new EffectColorGradientRandom(Gradient.Copy());
        public EffectColorGradientRandom Copy() => new(Gradient.Copy());

        public void Update(EffectColorGradientRandom src)
        {
            Gradient = src.Gradient.Copy();
        }

        public void Pull(EffectColorGradientRandom src)
        {
            Gradient.Pull(src.Gradient);
        }

        void IUpdatable<IEffectColor>.Update(IEffectColor src)
        {
            if (src is EffectColorGradientRandom value) Update(value);
        }
        void IMoveable<IEffectColor>.Pull(IEffectColor src)
        {
            if (src is EffectColorGradientRandom value) Pull(value);
        }

        public override bool Equals(object obj) => obj is EffectColorGradientRandom value && Equals(value);
        public override int GetHashCode() => Gradient != null ? Gradient.GetHashCode() : 0;
        
        public bool Equals(IEffectColor other) => other is EffectColorGradientRandom value && Equals(value);
        public bool Equals(EffectColorGradientRandom other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Gradient.Equals(other.Gradient);
            return result;
        }
    }
}