using System;
using BH.SDK.Models.Enums.Values;

namespace BH.SDK.Models.Interfaces.Values
{
    /// <summary> An authored whole number: a plain one, or a range the run draws from deterministically. </summary>
    public interface IInt : IModel<IInt>
    {
        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public IntType GetModelType();
    }
}