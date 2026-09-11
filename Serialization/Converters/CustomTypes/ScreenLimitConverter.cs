using System;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.CustomTypes
{
    /// <summary> Tags a screen limit with which of its forms it is. </summary>
    public class ScreenLimitConverter : JsonConverterCustomType<IScreenLimit, ScreenLimitType>
    {
        /// <summary> Which form the value is, read off the value itself. </summary>
        public override ScreenLimitType GetCustomType(IScreenLimit value) => value.GetModelType();
        /// <summary> The class each screen limit form is. </summary>
        public override Type GetType(ScreenLimitType customType)
        {
            return customType switch
            {
                ScreenLimitType.None => typeof(ScreenLimitNone),
                ScreenLimitType.Fixed => typeof(ScreenLimitFixed),
                ScreenLimitType.Bounds => typeof(ScreenLimitBounds),
                _ => Fallback(customType, typeof(ScreenLimitNone))
            };
        }
    }
}