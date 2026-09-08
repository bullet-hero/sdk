using System;
using BH.SDK.Models.Enums.Values;

namespace BH.SDK.Models.Interfaces.Values
{
    /// <summary> An authored 2D vector: a plain one, or an area the run draws a point from deterministically. </summary>
    public interface IVector2 : IModel<IVector2>
    {
        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public VectorType GetModelType();
    }
}