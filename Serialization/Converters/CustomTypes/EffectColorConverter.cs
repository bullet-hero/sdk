using System;
using BH.SDK.Models.Effects;
using BH.SDK.Models.Enums.Effects;
using BH.SDK.Models.Interfaces.Effects;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.CustomTypes
{
    /// <summary> Tags an effect's colour with which of its forms it is. </summary>
    public class EffectColorConverter : JsonConverterCustomType<IEffectColor, EffectColorType>
    {
        /// <summary> Which form the value is, read off the value itself. </summary>
        public override EffectColorType GetCustomType(IEffectColor value) => value.GetModelType();
        /// <summary> The class each effect colour form is. </summary>
        public override Type GetType(EffectColorType customType)
        {
            return customType switch
            {
                EffectColorType.Value => typeof(EffectColorValue),
                EffectColorType.GradientOverLife => typeof(EffectColorGradientOverLife),
                EffectColorType.GradientBySpeed => typeof(EffectColorGradientBySpeed),
                EffectColorType.RandomUniform => typeof(EffectColorRandomUniform),
                EffectColorType.RandomPerComponent => typeof(EffectColorRandomPerComponent),
                EffectColorType.GradientRandom => typeof(EffectColorGradientRandom),
                _ => Fallback(customType, typeof(EffectColorValue))
            };
        }
    }
}