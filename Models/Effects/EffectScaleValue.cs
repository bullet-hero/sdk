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
    /// Every particle keeps one fixed size for its whole life - the simplest IEffectScale variant.
    /// </summary>
    [RuleContainer]
    public class EffectScaleValue : IEffectScale, IModel<EffectScaleValue>
    {
        /// <summary> Particle size, per axis. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Scale)]
        public IVector2 Scale { get; set; }
        
        public EffectScaleType GetModelType() => EffectScaleType.Value;

        public EffectScaleValue()
        {
            Scale = new Vector2Value(
                EffectRules.Scale.A_X_Default, 
                EffectRules.Scale.A_Y_Default);
        }
        public EffectScaleValue(IVector2 scale)
        {
            Scale = scale;
        }
        public void Reset()
        {
            Scale = new Vector2Value(
                EffectRules.Scale.A_X_Default, 
                EffectRules.Scale.A_Y_Default);
        }

        public object Clone() => Copy();
        IEffectScale ICopyable<IEffectScale>.Copy() => new EffectScaleValue(Scale.Copy());
        public EffectScaleValue Copy() => new(Scale.Copy());

        public void Update(EffectScaleValue src)
        {
            Scale = src.Scale.Copy();
        }

        public void Pull(EffectScaleValue src)
        {
            Scale = Scale.PullFrom(src.Scale);
        }

        void IUpdatable<IEffectScale>.Update(IEffectScale src)
        {
            if (src is EffectScaleValue value) Update(value);
        }
        void IMoveable<IEffectScale>.Pull(IEffectScale src)
        {
            if (src is EffectScaleValue value) Pull(value);
        }

        public override bool Equals(object obj) => obj is EffectScaleValue value && Equals(value);
        public override int GetHashCode() => Scale != null ? Scale.GetHashCode() : 0;
        
        public bool Equals(IEffectScale other) => other is EffectScaleValue value && Equals(value);
        public bool Equals(EffectScaleValue other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Scale.Equals(other.Scale);
            return result;
        }
    }
}