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
    /// Tint read off a gradient by the particle's age - the usual way to fade a particle out, since
    /// the gradient's alpha track handles the fade without touching the color.
    /// </summary>
    [RuleContainer]
    public class EffectColorGradientOverLife : IEffectColor, IModel<EffectColorGradientOverLife>
    {
        /// <summary> Ramp sampled at normalized lifetime (0 = spawn, 1 = death). </summary>
        [RuleNotNull]
        [JsonProperty(Names.Gradient)]
        public GradientValue Gradient { get; set; }
        
        public EffectColorType GetModelType() => EffectColorType.GradientOverLife;

        public EffectColorGradientOverLife()
        {
            Gradient = EffectRules.GetGradient_Default();
        }
        public EffectColorGradientOverLife(GradientValue gradient)
        {
            Gradient = gradient;
        }
        public void Reset()
        {
            Gradient = EffectRules.GetGradient_Default();
        }

        public object Clone() => Copy();
        IEffectColor ICopyable<IEffectColor>.Copy() => new EffectColorGradientOverLife(Gradient.Copy());
        public EffectColorGradientOverLife Copy() => new(Gradient.Copy());

        public void Update(EffectColorGradientOverLife src)
        {
            Gradient = src.Gradient.Copy();
        }

        public void Pull(EffectColorGradientOverLife src)
        {
            Gradient.Pull(src.Gradient);
        }

        void IUpdatable<IEffectColor>.Update(IEffectColor src)
        {
            if (src is EffectColorGradientOverLife value) Update(value);
        }
        void IMoveable<IEffectColor>.Pull(IEffectColor src)
        {
            if (src is EffectColorGradientOverLife value) Pull(value);
        }

        public override bool Equals(object obj) => obj is EffectColorGradientOverLife value && Equals(value);
        public override int GetHashCode() => Gradient != null ? Gradient.GetHashCode() : 0;
        
        public bool Equals(IEffectColor other) => other is EffectColorGradientOverLife value && Equals(value);
        public bool Equals(EffectColorGradientOverLife other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Gradient.Equals(other.Gradient);
            return result;
        }
    }
}