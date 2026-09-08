using System;
using BH.SDK.Models.Enums.Meta;

namespace BH.SDK.Models.Interfaces.Values
{
    /// <summary> The terms a level is published under - a named licence, or one spelled out by hand. </summary>
    public interface ILicense : IModel<ILicense>
    {
        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public LicenseType GetModelType();
    }
}