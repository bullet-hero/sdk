using System;
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
    public class CustomLicense : ILicense, IModel<CustomLicense>
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
        public void Reset()
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
        
        public LicenseType GetModelType() => LicenseType.Custom;

        public object Clone() => Copy();
        ILicense ICopyable<ILicense>.Copy() => Copy();
        public CustomLicense Copy() => new(LicenseName, LicenseUrl, LicenseText,
            Aggressive, AllowsDistribution, AllowsModification, AllowsCommercialUse,
            RequiresAttribution, RequiresSourceDisclosure, RequiresSameLicense);



        public void Update(CustomLicense src)
        {
            LicenseName = src.LicenseName;
            LicenseUrl = src.LicenseUrl;
            LicenseText = src.LicenseText;
            Aggressive = src.Aggressive;
            AllowsDistribution = src.AllowsDistribution;
            AllowsModification = src.AllowsModification;
            AllowsCommercialUse = src.AllowsCommercialUse;
            RequiresAttribution = src.RequiresAttribution;
            RequiresSourceDisclosure = src.RequiresSourceDisclosure;
            RequiresSameLicense = src.RequiresSameLicense;
        }

        public void Pull(CustomLicense src)
        {
            LicenseName = src.LicenseName;
            LicenseUrl = src.LicenseUrl;
            LicenseText = src.LicenseText;
            Aggressive = src.Aggressive;
            AllowsDistribution = src.AllowsDistribution;
            AllowsModification = src.AllowsModification;
            AllowsCommercialUse = src.AllowsCommercialUse;
            RequiresAttribution = src.RequiresAttribution;
            RequiresSourceDisclosure = src.RequiresSourceDisclosure;
            RequiresSameLicense = src.RequiresSameLicense;
        }

        void IUpdatable<ILicense>.Update(ILicense src)
        {
            if (src is CustomLicense value) Update(value);
        }
        void IMoveable<ILicense>.Pull(ILicense src)
        {
            if (src is CustomLicense value) Pull(value);
        }

        public override bool Equals(object obj) => obj is CustomLicense value && Equals(value);
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(LicenseName);
            hashCode.Add(LicenseUrl);
            hashCode.Add(LicenseText);
            hashCode.Add(Aggressive);
            hashCode.Add(AllowsDistribution);
            hashCode.Add(AllowsModification);
            hashCode.Add(AllowsCommercialUse);
            hashCode.Add(RequiresAttribution);
            hashCode.Add(RequiresSourceDisclosure);
            hashCode.Add(RequiresSameLicense);
            return hashCode.ToHashCode();
        }
        
        public bool Equals(ILicense other) => other is CustomLicense value && Equals(value);
        public bool Equals(CustomLicense other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return LicenseName.Equals(other.LicenseName)
                   && LicenseUrl.Equals(other.LicenseUrl)
                   && LicenseText.Equals(other.LicenseText)
                   && Aggressive == other.Aggressive
                   && AllowsDistribution == other.AllowsDistribution
                   && AllowsModification == other.AllowsModification
                   && AllowsCommercialUse == other.AllowsCommercialUse
                   && RequiresAttribution == other.RequiresAttribution
                   && RequiresSourceDisclosure == other.RequiresSourceDisclosure
                   && RequiresSameLicense == other.RequiresSameLicense;
        }
    }
}