using System;
using BH.SDK.Models.Enums.Meta;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Values
{
    // Two records that both say "terms unknown" are no longer interchangeable, and that is the point
    // of Source: this type used to be fieldless, so Equals returned true for any pair of them. It
    // still means "nothing was declared", never "anything is permitted" - naming where the file came
    // from says nothing about what may be done with it, it only tells a moderator which conversation
    // to have.

    /// <summary>
    /// "Terms unknown" - the ILicense variant meaning nothing was declared, optionally naming the
    /// platform the work came from. The escape hatch of the ILicense family (NoSpecified / Typical /
    /// Custom); a resource carrying it is the one a takedown request lands on.
    /// </summary>
    [RuleContainer]
    public class NoSpecifiedLicense : ILicense, IModel<NoSpecifiedLicense>
    {
        /// <summary> Which platform the work was taken from, when that is known. Undefined is the
        /// normal case - most unlicensed works come from nowhere in particular. </summary>
        [RuleEnumValid]
        [JsonProperty(Names.Source)]
        public NoLicenseSourceType Source { get; set; }

        public NoSpecifiedLicense()
        {
            Source = NoLicenseSourceType.Undefined;
        }
        public NoSpecifiedLicense(NoLicenseSourceType source)
        {
            Source = source;
        }

        public LicenseType GetModelType() => LicenseType.NoSpecified;

        public void Reset()
        {
            Source = NoLicenseSourceType.Undefined;
        }

        public object Clone() => Copy();
        ILicense ICopyable<ILicense>.Copy() => Copy();
        public NoSpecifiedLicense Copy() => new(Source);

        public void Update(NoSpecifiedLicense src)
        {
            Source = src.Source;
        }

        public void Pull(NoSpecifiedLicense src)
        {
            Source = src.Source;
        }

        void IUpdatable<ILicense>.Update(ILicense src)
        {
            if (src is NoSpecifiedLicense value) Update(value);
        }
        void IMoveable<ILicense>.Pull(ILicense src)
        {
            if (src is NoSpecifiedLicense value) Pull(value);
        }

        public override int GetHashCode() => (int)Source;
        public override bool Equals(object obj) => obj is NoSpecifiedLicense value && Equals(value);

        public bool Equals(ILicense other) => other is NoSpecifiedLicense value && Equals(value);
        public bool Equals(NoSpecifiedLicense other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Source == other.Source;
        }
    }
}
