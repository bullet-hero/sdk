using System;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.CustomTypes
{
    /// <summary> Tags an RGB colour with whether it is literal or a theme reference. </summary>
    public class Color3Converter : JsonConverterCustomType<IColor3, ColorType>
    {
        /// <summary> Which form the value is, read off the value itself. </summary>
        public override ColorType GetCustomType(IColor3 value) => value.GetModelType();
        /// <summary> The class each colour form is. </summary>
        public override Type GetType(ColorType customType)
        {
            return customType switch
            {
                ColorType.Value => typeof(Color3Value),
                ColorType.ThemeRef => typeof(Color3ThemeRef),
                ColorType.RandomMinMax => typeof(Color3MinMax),
                _ => throw new ArgumentOutOfRangeException(nameof(customType), customType, null)
            };
        }
    }
}
