using System;
using BH.SDK.Models.Enums.Effects;

namespace BH.SDK.Models.Interfaces.Effects
{
    /// <summary> Which shape an effect emits, in whichever form the author picked. </summary>
    public interface IEffectShape : IModel<IEffectShape>
    {
        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public EffectShapeType GetModelType();
    }
}