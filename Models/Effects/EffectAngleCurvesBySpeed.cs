using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Effects;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Effects;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Effects
{
    /// <summary>
    /// Rotation driven by how fast the particle is moving rather than by its age - so the same
    /// particle can spin harder while accelerating and settle when it slows.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EffectAngleCurvesBySpeed : IEffectAngle, IModel<EffectAngleCurvesBySpeed>
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
    }
}