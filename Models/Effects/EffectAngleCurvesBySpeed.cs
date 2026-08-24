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
    /// Rotation driven by how fast the particle is moving rather than by its age - so the same
    /// particle can spin harder while accelerating and settle when it slows.
    /// </summary>
    [RuleContainer]
    public class EffectAngleCurvesBySpeed : IEffectAngle, IModel<EffectAngleCurvesBySpeed>
    {
        /// <summary> Angle over normalized speed (0 = SpeedRange min, 1 = max). </summary>
        [RuleNotNull]
        [JsonProperty(Names.Curve)]
        public CurveValue Curve { get; set; }

        /// <summary> Speed window mapped onto the curve's 0..1 axis; speeds outside it clamp. </summary>
        [RuleNotNull, RuleIVector2Ordered]
        [RuleIVector2InRange(EffectRules.SpeedRange_Min, EffectRules.SpeedRange_Max)]
        [JsonProperty(Names.SpeedRange)]
        public IVector2 SpeedRange { get; set; }
        
        public EffectAngleType GetModelType() => EffectAngleType.CurvesBySpeed;

        public EffectAngleCurvesBySpeed()
        {
            Curve = EffectRules.GetCurve_Default();
            SpeedRange = new Vector2Value(
                EffectRules.Angle.BySpeedRange_X_Default,
                EffectRules.Angle.BySpeedRange_Y_Default);
        }
        public EffectAngleCurvesBySpeed(CurveValue curve, IVector2 speedRange)
        {
            Curve = curve;
            SpeedRange = speedRange;
        }
        public void Reset()
        {
            Curve = EffectRules.GetCurve_Default();
            SpeedRange = new Vector2Value(
                EffectRules.Angle.BySpeedRange_X_Default,
                EffectRules.Angle.BySpeedRange_Y_Default);
        }

        public object Clone() => Copy();
        IEffectAngle ICopyable<IEffectAngle>.Copy() => new EffectAngleCurvesBySpeed(Curve.Copy(), SpeedRange.Copy());
        public EffectAngleCurvesBySpeed Copy() => new(Curve.Copy(), SpeedRange.Copy());

        public void Update(EffectAngleCurvesBySpeed src)
        {
            Curve = src.Curve.Copy();
            SpeedRange = src.SpeedRange.Copy();
        }

        public void Pull(EffectAngleCurvesBySpeed src)
        {
            Curve.Pull(src.Curve);
            SpeedRange = SpeedRange.PullFrom(src.SpeedRange);
        }

        void IUpdatable<IEffectAngle>.Update(IEffectAngle src)
        {
            if (src is EffectAngleCurvesBySpeed value) Update(value);
        }
        void IMoveable<IEffectAngle>.Pull(IEffectAngle src)
        {
            if (src is EffectAngleCurvesBySpeed value) Pull(value);
        }

        public override bool Equals(object obj) => obj is EffectAngleCurvesBySpeed value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(Curve, SpeedRange);
        
        public bool Equals(IEffectAngle other) => other is EffectAngleCurvesBySpeed value && Equals(value);
        public bool Equals(EffectAngleCurvesBySpeed other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Curve.Equals(other.Curve)
                         && SpeedRange.Equals(other.SpeedRange);
            return result;
        }
    }
}