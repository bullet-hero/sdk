using System;
using BH.SDK.Models.Enums.Values;

namespace BH.SDK.Models.Interfaces.Values
{
    /// <summary> An authored string: one text for everybody, or one per language. </summary>
    public interface IString : IModel<IString>
    {
        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public StringType GetModelType();
    }
}