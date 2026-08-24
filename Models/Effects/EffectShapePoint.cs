using System;
using BH.SDK.Models.Enums.Effects;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Effects;
using BH.SDK.Rules.Attributes;

namespace BH.SDK.Models.Effects
{
    /// <summary>
    /// Every particle spawns at the effect object's own origin. Fieldless - the object's transform
    /// is the shape, which also makes it the one shape with nothing to spread along.
    /// </summary>
    [RuleContainer]
    public class EffectShapePoint : IEffectShape, IModel<EffectShapePoint>
    {
        // None, inherit TRS from RectObject
        
        public EffectShapeType GetModelType() => EffectShapeType.Point;
        
        public void Reset() { }
        
        public object Clone() => Copy();
        IEffectShape ICopyable<IEffectShape>.Copy() => new EffectShapePoint();
        public EffectShapePoint Copy() => new();

        // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
        public void Update(EffectShapePoint src)
        {
        }

        public void Pull(EffectShapePoint src)
        {
        }

        void IUpdatable<IEffectShape>.Update(IEffectShape src)
        {
            if (src is EffectShapePoint value) Update(value);
        }
        void IMoveable<IEffectShape>.Pull(IEffectShape src)
        {
            if (src is EffectShapePoint value) Pull(value);
        }

        public override int GetHashCode() => base.GetHashCode();
        public override bool Equals(object obj) => obj is EffectShapePoint value && Equals(value);
        
        public bool Equals(IEffectShape other) => other is EffectShapePoint value && Equals(value);
        public bool Equals(EffectShapePoint other) => other is not null;
    }
}