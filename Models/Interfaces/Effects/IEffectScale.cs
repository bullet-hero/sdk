using System;
using BH.SDK.Models.Enums.Effects;

namespace BH.SDK.Models.Interfaces.Effects
{
    /// <summary> How an effect sizes what it emits, in whichever form the author picked. </summary>
    public interface IEffectScale : IModel<IEffectScale>
    {
        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public EffectScaleType GetModelType();
    }
}