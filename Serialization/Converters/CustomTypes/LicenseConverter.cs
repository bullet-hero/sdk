using System;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Serialization.Converters.Base;

namespace BH.SDK.Serialization.Converters.CustomTypes
{
    /// <summary> Tags a licence with whether it is a named one or spelled out by hand. </summary>
    public class LicenseConverter : JsonConverterCustomType<ILicense, LicenseType>
    {
        /// <summary> Which form the value is, read off the value itself. </summary>
        public override LicenseType GetCustomType(ILicense value) => value.GetModelType();
        /// <summary> The class each licence form is. </summary>
        public override Type GetType(LicenseType customType)
        {
            return customType switch
            {
                LicenseType.NoSpecified => typeof(NoSpecifiedLicense),
                LicenseType.Typical => typeof(TypicalLicense),
                LicenseType.Custom => typeof(CustomLicense),
                _ => throw new ArgumentOutOfRangeException(nameof(customType), customType, null)
            };
        }
    }
}
