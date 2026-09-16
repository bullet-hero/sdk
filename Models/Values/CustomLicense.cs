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
    // THE SEVEN PERMISSION FLAGS ARE GONE AND ARE NOT COMING BACK AS A SHORTER SET. They existed so
    // the game could "reason about a licence it has never seen", and nothing ever did: one branch in
    // PublishReadinessAnalyzer read AllowsDistribution, one read RequiresAttribution, and both were
    // asking the author to grade their own wording. A ticked box is not a fact about a licence - it
    // is a claim nobody can check, indistinguishable in the file from one that was read carefully,
    // and wrong more often than not, since whoever fills it in is rarely the rights holder.
    //
    // What is left is what an author can actually supply and a moderator can actually open: a name,
    // an address, and the wording itself. A custom licence is therefore never auto-approved by any
    // publish profile - a person reads it, which is what was really happening anyway.

    /// <summary>
    /// ILicense variant for terms that no preset covers - the licence's name, where it is published,
    /// and its full wording. The escape hatch of the ILicense family (NoSpecified / Typical /
    /// Custom).
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

        // UNCAPPED, and the only string in the format that is. Every other RuleStringMax truncates a
        // field whose meaning survives truncation - a title, a description, a tag. A licence body
        // does not: cutting it produces a different legal document that still reads like a whole one,
        // and no cap can be picked that is generous enough for every steward's wording and still
        // small enough to be worth having. The level file's own size limits are the real bound.

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