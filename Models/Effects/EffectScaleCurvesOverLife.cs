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
    /// Size driven by curves over the particle's age - what "pop in, shrink away" is made of. Two
    /// separate curves, so the axes can breathe out of step.
    /// </summary>
    [RuleContainer]
    public class EffectScaleCurvesOverLife : IEffectScale, IModel<EffectScaleCurvesOverLife>
    {
        /// <summary> Width over normalized lifetime. </summary>
        [RuleNotNull]
        [JsonProperty(Names.CurveX)]
        public CurveValue CurveX { get; set; }

        /// <summary> Height over normalized lifetime. </summary>
        [RuleNotNull]
        [JsonProperty(Names.CurveY)]
        public CurveValue CurveY { get; set; }
        
        public EffectScaleType GetModelType() => EffectScaleType.CurvesOverLife;
        
        public EffectScaleCurvesOverLife()
        {
            CurveX = EffectRules.GetCurve_Default();
            CurveY = EffectRules.GetCurve_Default();
        }
        public EffectScaleCurvesOverLife(CurveValue curveX, CurveValue curveY)
        {
            CurveX = curveX;
            CurveY = curveY;
        }
        public void Reset()
        {
            CurveX = EffectRules.GetCurve_Default();
            CurveY = EffectRules.GetCurve_Default();
        }

        public object Clone() => Copy();
        IEffectScale ICopyable<IEffectScale>.Copy() => new EffectScaleCurvesOverLife(CurveX.Copy(), CurveY.Copy());
        public EffectScaleCurvesOverLife Copy() => new(CurveX.Copy(), CurveY.Copy());

        public void Update(EffectScaleCurvesOverLife src)
        {
            CurveX = src.CurveX.Copy();
            CurveY = src.CurveY.Copy();
        }

        public void Pull(EffectScaleCurvesOverLife src)
        {
            CurveX.Pull(src.CurveX);
            CurveY.Pull(src.CurveY);
        }

        void IUpdatable<IEffectScale>.Update(IEffectScale src)
        {
            if (src is EffectScaleCurvesOverLife value) Update(value);
        }
        void IMoveable<IEffectScale>.Pull(IEffectScale src)
        {
            if (src is EffectScaleCurvesOverLife value) Pull(value);
        }

        public override bool Equals(object obj) => obj is EffectScaleCurvesOverLife value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(CurveX, CurveY);
        
        public bool Equals(IEffectScale other) => other is EffectScaleCurvesOverLife value && Equals(value);
        public bool Equals(EffectScaleCurvesOverLife other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = CurveX.Equals(other.CurveX)
                         && CurveY.Equals(other.CurveY);
            return result;
        }
    }
}