using System;
using BH.SDK.Models.Enums.Keyframes;
using BH.SDK.Models.Interfaces.Keyframes;
using BH.SDK.Models.Keyframes;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.CustomTypes
{
    public class FontSizeKeyConverter : JsonConverterCustomType<IFontSizeKey, FontSizeKeyType>
    {
        public override FontSizeKeyType GetCustomType(IFontSizeKey value) => value.GetModelType();
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
