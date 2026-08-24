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
    /// Size driven by how fast the particle moves - stretch it along its motion for a speed-line
    /// look without any per-particle scripting.
    /// </summary>
    [RuleContainer]
    public class EffectScaleCurvesBySpeed : IEffectScale, IModel<EffectScaleCurvesBySpeed>
    {
        /// <summary> Width over normalized speed. </summary>
        [RuleNotNull]
        [JsonProperty(Names.CurveX)]
        public CurveValue CurveX { get; set; }

        /// <summary> Height over normalized speed. </summary>
        [RuleNotNull]
        [JsonProperty(Names.CurveY)]
        public CurveValue CurveY { get; set; }

        /// <summary> Speed window mapped onto the curves' 0..1 axis. </summary>
        [RuleNotNull, RuleIVector2Ordered]
        [RuleIVector2InRange(EffectRules.SpeedRange_Min, EffectRules.SpeedRange_Max)]
        [JsonProperty(Names.SpeedRange)]
        public IVector2 SpeedRange { get; set; }

        public EffectScaleType GetModelType() => EffectScaleType.CurvesBySpeed;
        
        public EffectScaleCurvesBySpeed()
        {
            CurveX = EffectRules.GetCurve_Default();
            CurveY = EffectRules.GetCurve_Default();
            SpeedRange = new Vector2Value(
                EffectRules.Scale.BySpeedRange_X_Default,
                EffectRules.Scale.BySpeedRange_Y_Default);
        }
        public EffectScaleCurvesBySpeed(CurveValue curveX, CurveValue curveY, IVector2 speedRange)
        {
            CurveX = curveX;
            CurveY = curveY;
            SpeedRange = speedRange;
        }
        public void Reset()
        {
            CurveX = EffectRules.GetCurve_Default();
            CurveY = EffectRules.GetCurve_Default();
            SpeedRange = new Vector2Value(
                EffectRules.Scale.BySpeedRange_X_Default,
                EffectRules.Scale.BySpeedRange_Y_Default);
        }

        public object Clone() => Copy();
        IEffectScale ICopyable<IEffectScale>.Copy() => new EffectScaleCurvesBySpeed(CurveX.Copy(), CurveY.Copy(), SpeedRange.Copy());
        public EffectScaleCurvesBySpeed Copy() => new(CurveX.Copy(), CurveY.Copy(), SpeedRange.Copy());

        public void Update(EffectScaleCurvesBySpeed src)
        {
            CurveX = src.CurveX.Copy();
            CurveY = src.CurveY.Copy();
            SpeedRange = src.SpeedRange.Copy();
        }

        public void Pull(EffectScaleCurvesBySpeed src)
        {
            CurveX.Pull(src.CurveX);
            CurveY.Pull(src.CurveY);
            SpeedRange = SpeedRange.PullFrom(src.SpeedRange);
        }

        void IUpdatable<IEffectScale>.Update(IEffectScale src)
        {
            if (src is EffectScaleCurvesBySpeed value) Update(value);
        }
        void IMoveable<IEffectScale>.Pull(IEffectScale src)
        {
            if (src is EffectScaleCurvesBySpeed value) Pull(value);
        }

        public override bool Equals(object obj) => obj is EffectScaleCurvesBySpeed value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(CurveX, CurveY, SpeedRange);
        
        public bool Equals(IEffectScale other) => other is EffectScaleCurvesBySpeed value && Equals(value);
        public bool Equals(EffectScaleCurvesBySpeed other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = CurveX.Equals(other.CurveX)
                         && CurveY.Equals(other.CurveY)
                         && SpeedRange.Equals(other.SpeedRange);
            return result;
        }
    }
}