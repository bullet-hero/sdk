using System;
using BH.SDK.Models.Effects;
using BH.SDK.Models.Enums.Effects;
using BH.SDK.Models.Interfaces.Effects;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.CustomTypes
{
    /// <summary> Tags an effect's angle with which of its forms it is. </summary>
    public class EffectAngleConverter : JsonConverterCustomType<IEffectAngle, EffectAngleType>
    {
        /// <summary> Which form the value is, read off the value itself. </summary>
        public override EffectAngleType GetCustomType(IEffectAngle value) => value.GetModelType();
        /// <summary> The class each effect angle form is. </summary>
        public override Type GetType(EffectAngleType customType)
        {
            return customType switch
            {
                EffectAngleType.Value => typeof(EffectAngleValue),
                EffectAngleType.CurvesOverLife => typeof(EffectAngleCurvesOverLife),
                EffectAngleType.CurvesBySpeed => typeof(EffectAngleCurvesBySpeed),
                EffectAngleType.RandomUniform => typeof(EffectAngleRandomUniform),
                EffectAngleType.RandomPerComponent => typeof(EffectAngleRandomPerComponent),
                _ => throw new ArgumentOutOfRangeException(nameof(customType), customType, null)
            };
        }
    }
}