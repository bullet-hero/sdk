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
    /// Every particle keeps one fixed rotation for its whole life - the simplest IEffectAngle
    /// variant, and the default an effect starts from.
    /// </summary>
    [RuleContainer]
    public class EffectAngleValue : IEffectAngle, IModel<EffectAngleValue>
    {
        /// <summary> Rotation in degrees. Still an IFloat, so "fixed" can itself be a random draw
        /// made once per particle. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Angle)]
        public IFloat Angle { get; set; }
        
        public EffectAngleType GetModelType() => EffectAngleType.Value;

        public EffectAngleValue()
        {
            Angle = new FloatValue(EffectRules.Angle.A_Default);
        }
        public EffectAngleValue(float angle)
        {
            Angle = new FloatValue(angle);
        }
        public EffectAngleValue(IFloat angle)
        {
            Angle = angle;
        }
        public void Reset()
        {
            Angle = new FloatValue(EffectRules.Angle.A_Default);
        }

        public object Clone() => Copy();
        IEffectAngle ICopyable<IEffectAngle>.Copy() => new EffectAngleValue(Angle.Copy());
        public EffectAngleValue Copy() => new(Angle.Copy());

        public void Update(EffectAngleValue src)
        {
            Angle = src.Angle.Copy();
        }

        public void Pull(EffectAngleValue src)
        {
            Angle = Angle.PullFrom(src.Angle);
        }

        void IUpdatable<IEffectAngle>.Update(IEffectAngle src)
        {
            if (src is EffectAngleValue value) Update(value);
        }
        void IMoveable<IEffectAngle>.Pull(IEffectAngle src)
        {
            if (src is EffectAngleValue value) Pull(value);
        }

        public override bool Equals(object obj) => obj is EffectAngleValue value && Equals(value);
        public override int GetHashCode() => Angle != null ? Angle.GetHashCode() : 0;
        
        public bool Equals(IEffectAngle other) => other is EffectAngleValue value && Equals(value);
        public bool Equals(EffectAngleValue other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Angle.Equals(other.Angle);
            return result;
        }
    }
}