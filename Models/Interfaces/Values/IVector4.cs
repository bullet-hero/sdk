using System;
using BH.SDK.Models.Enums.Values;

namespace BH.SDK.Models.Interfaces.Values
{
    /// <summary> An authored 4D vector: a plain one, or a range the run draws from deterministically. </summary>
    public interface IVector4 : IModel<IVector4>
    {
        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public VectorType GetModelType();
    }
}