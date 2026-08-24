using System;
using BH.SDK.Models.Enums.Effects;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Effects;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Effects
{
    /// <summary>
    /// Tint blended between two colors by a single random factor - every particle lands somewhere on
    /// the straight line between A and B, so the palette stays coherent.
    /// </summary>
    [RuleContainer]
    public class EffectColorRandomUniform : IEffectColor, IModel<EffectColorRandomUniform>
    {
        /// <summary> One end of the blend. </summary>
        [RuleNotNull]
        [JsonProperty(Names.ColorA)]
        public IColor4 Color4A { get; set; }

        /// <summary> The other end of the blend. </summary>
        [RuleNotNull]
        [JsonProperty(Names.ColorB)]
        public IColor4 Color4B { get; set; }
        
        public EffectColorType GetModelType() => EffectColorType.RandomUniform;

        public EffectColorRandomUniform()
        {
            Color4A = new Color4Value(
                EffectRules.Color.A_R_Default,
                EffectRules.Color.A_G_Default,
                EffectRules.Color.A_B_Default,
                EffectRules.Color.A_A_Default);
            Color4B = new Color4Value(
                EffectRules.Color.B_R_Default,
                EffectRules.Color.B_G_Default,
                EffectRules.Color.B_B_Default,
                EffectRules.Color.B_A_Default);
        }
        public EffectColorRandomUniform(IColor4 color4A, IColor4 color4B)
        {
            Color4A = color4A;
            Color4B = color4B;
        }
        public void Reset()
        {
            Color4A = new Color4Value(
                EffectRules.Color.A_R_Default,
                EffectRules.Color.A_G_Default,
                EffectRules.Color.A_B_Default,
                EffectRules.Color.A_A_Default);
            Color4B = new Color4Value(
                EffectRules.Color.B_R_Default,
                EffectRules.Color.B_G_Default,
                EffectRules.Color.B_B_Default,
                EffectRules.Color.B_A_Default);
        }

        public object Clone() => Copy();
        IEffectColor ICopyable<IEffectColor>.Copy() => new EffectColorRandomUniform(Color4A.Copy(), Color4B.Copy());
        public EffectColorRandomUniform Copy() => new(Color4A.Copy(), Color4B.Copy());

        public void Update(EffectColorRandomUniform src)
        {
            Color4A = src.Color4A.Copy();
            Color4B = src.Color4B.Copy();
        }

        public void Pull(EffectColorRandomUniform src)
        {
            Color4A = Color4A.PullFrom(src.Color4A);
            Color4B = Color4B.PullFrom(src.Color4B);
        }

        void IUpdatable<IEffectColor>.Update(IEffectColor src)
        {
            if (src is EffectColorRandomUniform value) Update(value);
        }
        void IMoveable<IEffectColor>.Pull(IEffectColor src)
        {
            if (src is EffectColorRandomUniform value) Pull(value);
        }

        public override bool Equals(object obj) => obj is EffectColorRandomUniform value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(Color4A, Color4B);
        
        public bool Equals(IEffectColor other) => other is EffectColorRandomUniform value && Equals(value);
        public bool Equals(EffectColorRandomUniform other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Color4A.Equals(other.Color4A)
                         && Color4B.Equals(other.Color4B);
            return result;
        }
    }
}