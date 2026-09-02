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
    /// Rotation driven by a curve over the particle's age - what spinning debris that slows down
    /// as it fades is made of.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EffectAngleCurvesOverLife : IEffectAngle, IModel<EffectAngleCurvesOverLife>
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
    }
}