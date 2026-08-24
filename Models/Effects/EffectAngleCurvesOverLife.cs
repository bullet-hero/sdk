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
    /// Rotation driven by a curve over the particle's age - what spinning debris that slows down
    /// as it fades is made of.
    /// </summary>
    [RuleContainer]
    public class EffectAngleCurvesOverLife : IEffectAngle, IModel<EffectAngleCurvesOverLife>
    {
        /// <summary> Angle over normalized lifetime (0 = spawn, 1 = death). </summary>
        [RuleNotNull]
        [JsonProperty(Names.Curve)]
        public CurveValue Curve { get; set; }

        public EffectAngleType GetModelType() => EffectAngleType.CurvesOverLife;
        
        public EffectAngleCurvesOverLife()
        {
            Curve = EffectRules.GetCurve_Default();
        }
        public EffectAngleCurvesOverLife(CurveValue curve)
        {
            Curve = curve;
        }
        public void Reset()
        {
            Curve = EffectRules.GetCurve_Default();
        }

        public object Clone() => Copy();
        IEffectAngle ICopyable<IEffectAngle>.Copy() => new EffectAngleCurvesOverLife(Curve.Copy());
        public EffectAngleCurvesOverLife Copy() => new(Curve.Copy());

        public void Update(EffectAngleCurvesOverLife src)
        {
            Curve = src.Curve.Copy();
        }

        public void Pull(EffectAngleCurvesOverLife src)
        {
            Curve.Pull(src.Curve);
        }

        void IUpdatable<IEffectAngle>.Update(IEffectAngle src)
        {
            if (src is EffectAngleCurvesOverLife value) Update(value);
        }
        void IMoveable<IEffectAngle>.Pull(IEffectAngle src)
        {
            if (src is EffectAngleCurvesOverLife value) Pull(value);
        }

        public override bool Equals(object obj) => obj is EffectAngleCurvesOverLife value && Equals(value);
        public override int GetHashCode() => Curve != null ? Curve.GetHashCode() : 0;
        
        public bool Equals(IEffectAngle other) => other is EffectAngleCurvesOverLife value && Equals(value);
        public bool Equals(EffectAngleCurvesOverLife other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Curve.Equals(other.Curve);
            return result;
        }
    }
}