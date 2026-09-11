using System;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Objects;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.CustomTypes
{
    /// <summary> Tags a level object with which kind it is - shape, text, prefab placement, empty. </summary>
    public class ObjectConverter : JsonConverterCustomType<RectObject, ObjectType>
    {
        /// <summary> Which form the value is, read off the value itself. </summary>
        public override ObjectType GetCustomType(RectObject value) => value.GetModelType();
        /// <summary> The class each object form is. </summary>
        public override Type GetType(ObjectType customType)
        {
            return customType switch
            {
                ObjectType.RectObject => typeof(RectObject),
                ObjectType.ShapeObject => typeof(ShapeObject),
                ObjectType.TextObject => typeof(TextObject),
                ObjectType.EffectObject => typeof(EffectObject),
                ObjectType.PrefabObject => typeof(PrefabObject),
                _ => Fallback(customType, typeof(RectObject))
            };
        }
    }
}