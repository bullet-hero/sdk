using System;
using BH.SDK.Models.Enums.Effects;

namespace BH.SDK.Models.Interfaces.Effects
{
    /// <summary> An angle an effect is authored with, in whichever form the author picked. </summary>
    public interface IEffectAngle : IModel<IEffectAngle>
    {
        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public EffectAngleType GetModelType();
    }
}