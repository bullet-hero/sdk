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
    /// <summary>
    /// ILicense variant for terms that no preset covers - the license text itself plus the individual
    /// permissions spelled out as flags, so the game can reason about a license it has never seen.
    /// The escape hatch of the ILicense family (NoSpecified / Typical / Custom).
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

        /// <summary> Full license wording embedded in the level, so it survives the URL going dead. </summary>
        [RuleNotNull, RuleStringMax(ValueRules.MaxLicenseText)]
        [JsonProperty(Names.Text)]
        public string LicenseText { get; set; }

        /// <summary>
        /// If true - "Copyleft", if false - "Permissive"
        /// </summary>
        [JsonProperty(Names.Aggressive)]
        public bool Aggressive { get; set; }

        /// <summary> May the work be shared as part of a level at all - the flag that decides whether
        /// the level can be published anywhere. </summary>
        [JsonProperty(Names.AllowsDistribution)]
        public bool AllowsDistribution { get; set; }

        /// <summary> May the work be altered (recolored, cropped, remixed) before use. </summary>
        [JsonProperty(Names.AllowsModification)]
        public bool AllowsModification { get; set; }

        /// <summary> May the work ship in something sold or monetized. </summary>
        [JsonProperty(Names.AllowsCommercialUse)]
        public bool AllowsCommercialUse { get; set; }

        /// <summary> Must the author be credited - what makes ResourceMeta.ResourceAuthors mandatory
        /// rather than decorative. </summary>
        [JsonProperty(Names.RequiresAttribution)]
        public bool RequiresAttribution { get; set; }

        /// <summary> Must sources/originals be published alongside the derived work. </summary>
        [JsonProperty(Names.RequiresSourceDisclosure)]
        public bool RequiresSourceDisclosure { get; set; }

        /// <summary> Must derivatives carry this same license - the concrete consequence of
        /// Aggressive being copyleft. </summary>
        [JsonProperty(Names.RequiresSameLicense)]
        public bool RequiresSameLicense { get; set; }

        public CustomLicense()
        {
            LicenseName = string.Empty;
            LicenseUrl = string.Empty;
            LicenseText = string.Empty;
            Aggressive = false;
            AllowsDistribution = false;
            AllowsModification = false;
            AllowsCommercialUse = false;
            RequiresAttribution = false;
            RequiresSourceDisclosure = false;
            RequiresSameLicense = false;
        }
        public CustomLicense(string licenseName, string licenseUrl, string licenseText,
            bool aggressive, bool allowsDistribution, bool allowsModification, bool allowsCommercialUse,
            bool requiresAttribution, bool requiresSourceDisclosure, bool requiresSameLicense)
        {
            LicenseName = licenseName;
            LicenseUrl = licenseUrl;
            LicenseText = licenseText;
            Aggressive = aggressive;
            AllowsDistribution = allowsDistribution;
            AllowsModification = allowsModification;
            AllowsCommercialUse = allowsCommercialUse;
            RequiresAttribution = requiresAttribution;
            RequiresSourceDisclosure = requiresSourceDisclosure;
            RequiresSameLicense = requiresSameLicense;
        }
        
        public LicenseType GetModelType() => LicenseType.Custom;
    }
}