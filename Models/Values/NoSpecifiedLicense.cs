using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Values
{
    // Source makes two "terms unknown" records distinct. Still means "nothing declared", never "anything
    // is permitted": it tells a moderator which conversation to have.

    /// <summary>
    /// "Terms unknown": nothing was declared, optionally naming the platform the work came from. The
    /// resource a takedown request lands on.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class NoSpecifiedLicense : ILicense, IModel<NoSpecifiedLicense>
    {
        /// <summary> Which platform the work was taken from, when that is known. Undefined is the
        /// normal case - most unlicensed works come from nowhere in particular. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.Source)]
        public NoLicenseSourceType Source { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public NoSpecifiedLicense()
        {
            Source = NoLicenseSourceType.Undefined;
        }
        /// <summary> Built from its source. </summary>
        public NoSpecifiedLicense(NoLicenseSourceType source)
        {
            Source = source;
        }

        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public LicenseType GetModelType() => LicenseType.NoSpecified;
    }
}
