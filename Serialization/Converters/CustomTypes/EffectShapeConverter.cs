using System;
using BH.SDK.Models.Effects;
using BH.SDK.Models.Enums.Effects;
using BH.SDK.Models.Interfaces.Effects;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.CustomTypes
{
    /// <summary> Tags an effect's shape with which of its forms it is. </summary>
    public class EffectShapeConverter : JsonConverterCustomType<IEffectShape, EffectShapeType>
    {
        /// <summary> Which form the value is, read off the value itself. </summary>
        public override EffectShapeType GetCustomType(IEffectShape value) => value.GetModelType();
        /// <summary> The class each effect shape form is. </summary>
        public override Type GetType(EffectShapeType customType)
        {
            return customType switch
            {
                EffectShapeType.Point => typeof(EffectShapePoint),
                EffectShapeType.Circle => typeof(EffectShapeCircle),
                EffectShapeType.Rectangle => typeof(EffectShapeRectangle),
                EffectShapeType.Line => typeof(EffectShapeLine),
                EffectShapeType.Cone => typeof(EffectShapeCone),
                EffectShapeType.Torus => typeof(EffectShapeTorus),
                _ => throw new ArgumentOutOfRangeException(nameof(customType), customType, null)
            };
        }
    }
}