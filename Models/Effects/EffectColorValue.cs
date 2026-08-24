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
    /// One constant tint for every particle - the simplest IEffectColor variant.
    /// </summary>
    [RuleContainer]
    public class EffectColorValue : IEffectColor, IModel<EffectColorValue>
    {
        /// <summary> Particle tint. Polymorphic, so it can be a ThemeRef and follow the level's
        /// active palette. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Color)]
        public IColor4 Color4 { get; set; }
        
        public EffectColorType GetModelType() => EffectColorType.Value;

        public EffectColorValue()
        {
            Color4 = new Color4Value(
                EffectRules.Color.A_R_Default,
                EffectRules.Color.A_G_Default,
                EffectRules.Color.A_B_Default,
                EffectRules.Color.A_A_Default);
        }
        public EffectColorValue(IColor4 color4)
        {
            Color4 = color4;
        }
        public void Reset()
        {
            Color4 = new Color4Value(
                EffectRules.Color.A_R_Default,
                EffectRules.Color.A_G_Default,
                EffectRules.Color.A_B_Default,
                EffectRules.Color.A_A_Default);
        }

        public object Clone() => Copy();
        IEffectColor ICopyable<IEffectColor>.Copy() => new EffectColorValue(Color4.Copy());
        public EffectColorValue Copy() => new(Color4.Copy());
        
        public void Update(EffectColorValue src)
        {
            Color4 = src.Color4.Copy();
        }

        public void Pull(EffectColorValue src)
        {
            Color4 = Color4.PullFrom(src.Color4);
        }

        void IUpdatable<IEffectColor>.Update(IEffectColor src)
        {
            if (src is EffectColorValue value) Update(value);
        }
        void IMoveable<IEffectColor>.Pull(IEffectColor src)
        {
            if (src is EffectColorValue value) Pull(value);
        }

        public bool Equals(IEffectColor other) => other is EffectColorValue value && Equals(value);

        public override bool Equals(object obj) => obj is EffectColorValue value && Equals(value);
        public override int GetHashCode() => Color4 != null ? Color4.GetHashCode() : 0;

        public bool Equals(EffectColorValue other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Color4.Equals(other.Color4);
            return result;
        }
    }
}