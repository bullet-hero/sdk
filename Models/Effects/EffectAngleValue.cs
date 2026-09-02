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
    /// Every particle keeps one fixed rotation for its whole life - the simplest IEffectAngle
    /// variant, and the default an effect starts from.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EffectAngleValue : IEffectAngle, IModel<EffectAngleValue>
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
    }
}