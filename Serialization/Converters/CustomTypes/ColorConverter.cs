using System;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.CustomTypes
{
    /// <summary> Tags an RGBA colour with whether it is literal or a theme reference. </summary>
    public class ColorConverter : JsonConverterCustomType<IColor4, ColorType>
    {
        /// <summary> Which form the value is, read off the value itself. </summary>
        public override ColorType GetCustomType(IColor4 value) => value.GetModelType();
        /// <summary> The class each colour form is. </summary>
        public override Type GetType(ColorType customType)
        {
            return customType switch
            {
                ColorType.Value => typeof(Color4Value),
                ColorType.ThemeRef => typeof(Color4ThemeRef),
                ColorType.RandomMinMax => typeof(Color4MinMax),
                _ => Fallback(customType, typeof(Color4Value))
            };
        }
    }
}