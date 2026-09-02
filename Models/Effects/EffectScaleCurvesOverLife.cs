using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Effects;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Effects;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Effects
{
    /// <summary>
    /// Size driven by curves over the particle's age - what "pop in, shrink away" is made of. Two
    /// separate curves, so the axes can breathe out of step.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EffectScaleCurvesOverLife : IEffectScale, IModel<EffectScaleCurvesOverLife>
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
    }
}