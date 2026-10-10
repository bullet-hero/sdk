using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Values
{
    // No permission flags: a ticked box is a claim nobody can check. A name, an address and the wording,
    // and a custom licence is never auto-approved by a publish profile. Models/Values/CLAUDE.md

    /// <summary>
    /// ILicense variant for terms no preset covers: the licence's name, where it is published, and its full
    /// wording. The escape hatch of the ILicense family (NoSpecified / Typical / Custom).
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class CustomLicense : ILicense, IModel<CustomLicense>
    {
        /// <summary> Display name of the license ("My Studio EULA v2"). </summary>
        [RuleNotNull, RuleStringMax(ValueRules.MaxLicenseName)]
        [JsonProperty(Names.Name)]
        public string LicenseName { get; set; }

        /// <summary> Where the authoritative wording is published. </summary>
        [RuleNotNull, RuleStringMax(ValueRules.MaxUrl)]
        [JsonProperty(Names.Url)]
        public string LicenseUrl { get; set; }

        // UNCAPPED, the only string in the format that is: truncating a licence yields a different legal
        // document that still reads whole. The level file's own size limits are the bound.

        /// <summary> Full license wording embedded in the level, so it survives the URL going dead. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Text)]
        public string LicenseText { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public CustomLicense()
        {
            LicenseName = string.Empty;
            LicenseUrl = string.Empty;
            LicenseText = string.Empty;
        }
        /// <summary> Every member at once, in declaration order. </summary>
        public CustomLicense(string licenseName, string licenseUrl, string licenseText)
        {
            LicenseName = licenseName;
            LicenseUrl = licenseUrl;
            LicenseText = licenseText;
        }

        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public LicenseType GetModelType() => LicenseType.Custom;
    }
}