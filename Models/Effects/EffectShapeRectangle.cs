using System;
using BH.SDK.Models.Enums.Effects;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Effects;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Effects
{
    /// <summary>
    /// Emitter shape spawning particles anywhere inside an axis-aligned box. The one shape of the
    /// family with no IEffectShapeSpread - a filled area has no rim to walk along.
    /// </summary>
    [RuleContainer]
    public class EffectShapeRectangle : IEffectShape, IModel<EffectShapeRectangle>
    {
        /// <summary> Full width/height of the spawn box, centered on the effect object. </summary>
        [RuleNotNull, RuleIVector2Min(EffectRules.Shape.BoxSize_Min)]
        [JsonProperty(Names.Size)]
        public IVector2 Size { get; set; }

        public EffectShapeRectangle()
        {
            Size = new Vector2Value(
                EffectRules.Shape.BoxSize_X_Default,
                EffectRules.Shape.BoxSize_Y_Default);
        }
        public EffectShapeRectangle(IVector2 size)
        {
            Size = size;
        }
        public void Reset()
        {
            Size = new Vector2Value(
                EffectRules.Shape.BoxSize_X_Default,
                EffectRules.Shape.BoxSize_Y_Default);
        }

        public object Clone() => Copy();
        public EffectShapeType GetModelType() => EffectShapeType.Rectangle;
        IEffectShape ICopyable<IEffectShape>.Copy() => new EffectShapeRectangle(Size.Copy());
        public EffectShapeRectangle Copy() => new(Size.Copy());

        public void Update(EffectShapeRectangle src)
        {
            Size = src.Size.Copy();
        }

        public void Pull(EffectShapeRectangle src)
        {
            Size = Size.PullFrom(src.Size);
        }

        void IUpdatable<IEffectShape>.Update(IEffectShape src)
        {
            if (src is EffectShapeRectangle value) Update(value);
        }
        void IMoveable<IEffectShape>.Pull(IEffectShape src)
        {
            if (src is EffectShapeRectangle value) Pull(value);
        }

        public override bool Equals(object obj) => obj is EffectShapeRectangle value && Equals(value);
        public override int GetHashCode() => Size != null ? Size.GetHashCode() : 0;
        
        public bool Equals(IEffectShape other) => other is EffectShapeRectangle value && Equals(value);
        public bool Equals(EffectShapeRectangle other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Size.Equals(other.Size);
            return result;
        }
    }
}