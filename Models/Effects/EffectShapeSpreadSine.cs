using System;
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
    public class EffectShapeSpreadSine : IEffectShapeSpread, IModel<EffectShapeSpreadSine>
    {
        public EffectShapeSpreadType GetModelType() => EffectShapeSpreadType.Sine;
        
        public void Reset() { }
        
        public object Clone() => Copy();
        IEffectShapeSpread ICopyable<IEffectShapeSpread>.Copy() => new EffectShapeSpreadSine();
        public EffectShapeSpreadSine Copy() => new();

        // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
        public void Update(EffectShapeSpreadSine src)
        {
        }

        public void Pull(EffectShapeSpreadSine src)
        {
        }

        void IUpdatable<IEffectShapeSpread>.Update(IEffectShapeSpread src)
        {
            if (src is EffectShapeSpreadSine value) Update(value);
        }
        void IMoveable<IEffectShapeSpread>.Pull(IEffectShapeSpread src)
        {
            if (src is EffectShapeSpreadSine value) Pull(value);
        }

        public override int GetHashCode() => base.GetHashCode();
        public override bool Equals(object obj) => obj is EffectShapeSpreadSine value && Equals(value);
        
        public bool Equals(IEffectShapeSpread other) => other is EffectShapeSpreadSine value && Equals(value);
        public bool Equals(EffectShapeSpreadSine other) => other is not null;
    }
}