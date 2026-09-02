using System;
using BH.SDK.Models.Attributes;
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
    [GenerateModel]
    public sealed partial class EffectShapePoint : IEffectShape, IModel<EffectShapePoint>
    {
        // None, inherit TRS from RectObject
        
        public EffectShapeType GetModelType() => EffectShapeType.Point;
        
        // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
    }
}