using System;
using BH.SDK.Models.Enums.Values;

namespace BH.SDK.Models.Interfaces.Values
{
    /// <summary> An authored 3D vector: a plain one, or a volume the run draws a point from deterministically. </summary>
    public interface IVector3 : IModel<IVector3>
    {
        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public VectorType GetModelType();
    }
}