using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Values
{
    /// <summary>
    /// A well-known license picked from a list (CC family, MIT, ...) - one enum instead of the full
    /// text CustomLicense has to carry, since the game already knows what these terms mean.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class TypicalLicense : ILicense, IModel<TypicalLicense>
    {
        /// <summary> Which preset license applies. </summary>
        [RuleEnumValid(TypicalLicenseType.CC_BY_NC_4_0)]
        [JsonProperty(Names.LicenseType)]
        public TypicalLicenseType Type { get; set; }

        public TypicalLicense()
        {
            Type = TypicalLicenseType.CC_BY_NC_4_0;
        }
        public TypicalLicense(TypicalLicenseType type)
        {
            Type = type;
        }

        public LicenseType GetModelType() => LicenseType.Typical;
    }
}