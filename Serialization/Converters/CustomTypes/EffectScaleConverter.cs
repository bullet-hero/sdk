using System;
using BH.SDK.Models.Effects;
using BH.SDK.Models.Enums.Effects;
using BH.SDK.Models.Interfaces.Effects;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.CustomTypes
{
    /// <summary> Tags an effect's scale with which of its forms it is. </summary>
    public class EffectScaleConverter : JsonConverterCustomType<IEffectScale, EffectScaleType>
    {
        /// <summary> Which form the value is, read off the value itself. </summary>
        public override EffectScaleType GetCustomType(IEffectScale value) => value.GetModelType();
        /// <summary> The class each effect scale form is. </summary>
        public override Type GetType(EffectScaleType customType)
        {
            return customType switch
            {
                EffectScaleType.Value => typeof(EffectScaleValue),
                EffectScaleType.CurvesOverLife => typeof(EffectScaleCurvesOverLife),
                EffectScaleType.CurvesBySpeed => typeof(EffectScaleCurvesBySpeed),
                EffectScaleType.RandomUniform => typeof(EffectScaleRandomUniform),
                EffectScaleType.RandomPerComponent => typeof(EffectScaleRandomPerComponent),
                _ => Fallback(customType, typeof(EffectScaleValue))
            };
        }
    }
}