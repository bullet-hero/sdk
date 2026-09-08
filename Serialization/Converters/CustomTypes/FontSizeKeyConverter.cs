using System;
using BH.SDK.Models.Enums.Keyframes;
using BH.SDK.Models.Interfaces.Keyframes;
using BH.SDK.Models.Keyframes;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.CustomTypes
{
    /// <summary> Tags a font-size keyframe with whether it holds a number or a fit rule. </summary>
    public class FontSizeKeyConverter : JsonConverterCustomType<IFontSizeKey, FontSizeKeyType>
    {
        /// <summary> Which form the value is, read off the value itself. </summary>
        public override FontSizeKeyType GetCustomType(IFontSizeKey value) => value.GetModelType();
        /// <summary> The class each font-size keyframe form is. </summary>
        public override Type GetType(FontSizeKeyType customType)
        {
            return customType switch
            {
                FontSizeKeyType.Value => typeof(FontSizeKey),
                FontSizeKeyType.Auto => typeof(AutoFontSizeKey),
                _ => throw new ArgumentOutOfRangeException(nameof(customType), customType, null)
            };
        }
    }
}
