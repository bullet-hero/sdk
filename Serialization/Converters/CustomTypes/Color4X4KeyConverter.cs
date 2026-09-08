using System;
using BH.SDK.Models.Enums.Keyframes;
using BH.SDK.Models.Interfaces.Keyframes;
using BH.SDK.Models.Keyframes;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.CustomTypes
{
    /// <summary> Tags a four-colour keyframe with which of its forms it is. </summary>
    public class Color4X4KeyConverter : JsonConverterCustomType<IColor4X4Key, Color4X4KeyType>
    {
        /// <summary> Which form the value is, read off the value itself. </summary>
        public override Color4X4KeyType GetCustomType(IColor4X4Key value) => value.GetModelType();
        /// <summary> The class each four-colour keyframe form is. </summary>
        public override Type GetType(Color4X4KeyType customType)
        {
            return customType switch
            {
                Color4X4KeyType.Value => typeof(Color4Key),
                Color4X4KeyType.Horizontal => typeof(ColorHorizontalKey),
                Color4X4KeyType.Vertical => typeof(ColorVerticalKey),
                Color4X4KeyType.BariCentrical => typeof(Color4X4Key),
                _ => throw new ArgumentOutOfRangeException(nameof(customType), customType, null)
            };
        }
    }
}
