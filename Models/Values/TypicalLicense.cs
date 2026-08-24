using System;
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
    public class TypicalLicense : ILicense, IModel<TypicalLicense>
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
        public void Reset()
        {
            Type = TypicalLicenseType.CC_BY_NC_4_0;
        }

        public LicenseType GetModelType() => LicenseType.Typical;
        

        public object Clone() => Copy();
        ILicense ICopyable<ILicense>.Copy() => Copy();
        public TypicalLicense Copy() => new(Type);
        
        public void Update(TypicalLicense src)
        {
            Type = src.Type;
        }

        public void Pull(TypicalLicense src)
        {
            Type = src.Type;
        }

        void IUpdatable<ILicense>.Update(ILicense src)
        {
            if (src is TypicalLicense value) Update(value);
        }
        void IMoveable<ILicense>.Pull(ILicense src)
        {
            if (src is TypicalLicense value) Pull(value);
        }

        public override bool Equals(object obj) => obj is TypicalLicense value && Equals(value);
        public override int GetHashCode() => (int)Type;
        
        public bool Equals(ILicense other) => other is TypicalLicense value && Equals(value);
        public bool Equals(TypicalLicense other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Type == other.Type;
            return result;
        }
    }
}