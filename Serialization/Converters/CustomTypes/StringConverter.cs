using System;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.CustomTypes
{
    /// <summary> Tags an authored string with whether it is one text or one per language. </summary>
    public class StringConverter : JsonConverterCustomType<IString, StringType>
    {
        /// <summary> Which form the value is, read off the value itself. </summary>
        public override StringType GetCustomType(IString value) => value.GetModelType();
        /// <summary> The class each string form is. </summary>
        public override Type GetType(StringType customType)
        {
            return customType switch
            {
                StringType.Value => typeof(StringValue),
                StringType.Localized => typeof(StringLocalized),
                _ => throw new ArgumentOutOfRangeException(nameof(customType), customType, null)
            };
        }
    }
}