using System;
using BH.SDK.Models.Enums.Effects;

namespace BH.SDK.Models.Interfaces.Effects
{
    /// <summary> How an effect spreads its shapes over the area it covers. </summary>
    public interface IEffectShapeSpread : IModel<IEffectShapeSpread>
    {
        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public EffectShapeSpreadType GetModelType();
    }
}