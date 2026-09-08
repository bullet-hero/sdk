using System;
using BH.SDK.Models.Enums.Effects;

namespace BH.SDK.Models.Interfaces.Effects
{
    /// <summary> A colour an effect is authored with, in whichever form the author picked. </summary>
    public interface IEffectColor : IModel<IEffectColor>
    {
        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public EffectColorType GetModelType();
    }
}