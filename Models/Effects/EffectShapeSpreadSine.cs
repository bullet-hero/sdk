using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Effects;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Effects;
using BH.SDK.Rules.Attributes;

namespace BH.SDK.Models.Effects
{
    /// <summary>
    /// Spawn point follows a sine, easing at the turnarounds instead of moving at a constant rate.
    /// Fieldless - unlike Loop/PingPong it takes neither spread nor speed, so the motion is fixed.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EffectShapeSpreadSine : IEffectShapeSpread, IModel<EffectShapeSpreadSine>
    {
        public EffectShapeSpreadType GetModelType() => EffectShapeSpreadType.Sine;
        
        // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
    }
}