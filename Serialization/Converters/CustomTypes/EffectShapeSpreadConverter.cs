using System;
using BH.SDK.Models.Effects;
using BH.SDK.Models.Enums.Effects;
using BH.SDK.Models.Interfaces.Effects;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.CustomTypes
{
    /// <summary> Tags an effect's spread with which of its forms it is. </summary>
    public class EffectShapeSpreadConverter : JsonConverterCustomType<IEffectShapeSpread, EffectShapeSpreadType>
    {
        /// <summary> Which form the value is, read off the value itself. </summary>
        public override EffectShapeSpreadType GetCustomType(IEffectShapeSpread value) => value.GetModelType();
        /// <summary> The class each effect spread form is. </summary>
        public override Type GetType(EffectShapeSpreadType customType)
        {
            return customType switch
            {
                EffectShapeSpreadType.Random => typeof(EffectShapeSpreadRandom),
                EffectShapeSpreadType.Loop => typeof(EffectShapeSpreadLoop),
                EffectShapeSpreadType.PingPong => typeof(EffectShapeSpreadPingPong),
                EffectShapeSpreadType.Sine => typeof(EffectShapeSpreadSine),
                _ => throw new ArgumentOutOfRangeException(nameof(customType), customType, null)
            };
        }
    }
}