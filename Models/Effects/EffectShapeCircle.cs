using System;
using BH.SDK.Models.Enums.Effects;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Effects;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Effects
{
    /// <summary>
    /// Emitter shape spawning particles on (or inside) a ring - the backbone of radial bullet
    /// patterns, since Arc plus Spread together decide where along the ring the next particle lands.
    /// </summary>
    [RuleContainer]
    public class EffectShapeCircle : IEffectShape, IModel<EffectShapeCircle>
    {
        /// <summary> Distance from the center to the ring, along the horizontal axis. </summary>
        [RuleNotNull, RuleIFloatMin(EffectRules.Shape.CircleRadius_Min)]
        [JsonProperty(Names.Radius)]
        public IFloat Radius { get; set; }

        // A RATIO, not a second radius, and that is what makes it additive: 1 is a circle, so a file
        // written before this property existed reads back exactly as it used to. An absolute second
        // radius would default to 1 as well and turn every non-unit-radius ring into an ellipse.

        /// <summary> Vertical semi-axis as a multiple of <see cref="Radius"/>: 1 is a circle,
        /// anything else an ellipse. </summary>
        [RuleNotNull, RuleIFloatInRange(EffectRules.Shape.CircleAspect_Min, EffectRules.Shape.CircleAspect_Max)]
        [JsonProperty(Names.Aspect)]
        public IFloat Aspect { get; set; }

        /// <summary> How far inward the ring is filled: 0 is a bare outline, 1 a full disc. </summary>
        [RuleNotNull, RuleIFloatInRange(EffectRules.Shape.CircleThickness_Min, EffectRules.Shape.CircleThickness_Max)]
        [JsonProperty(Names.Thickness)]
        public IFloat Thickness { get; set; }

        /// <summary> Portion of the circle actually used, in radians, measured counter-clockwise
        /// from the +X axis - less than a full turn makes a fan. </summary>
        [RuleNotNull, RuleIFloatInRange(EffectRules.Shape.Arc_Min, EffectRules.Shape.Arc_Max)]
        [JsonProperty(Names.Arc)]
        public IFloat Arc { get; set; }

        /// <summary> How successive particles walk the arc: randomly, looping, ping-pong or sine.
        /// This is what turns a static ring into a rotating or sweeping pattern. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Spread)]
        public IEffectShapeSpread Spread { get; set; }
        
        public EffectShapeType GetModelType() => EffectShapeType.Circle;

        public EffectShapeCircle()
        {
            Radius = new FloatValue(EffectRules.Shape.CircleRadius_Default);
            Aspect = new FloatValue(EffectRules.Shape.CircleAspect_Default);
            Thickness = new FloatValue(EffectRules.Shape.CircleThickness_Default);
            Arc = new FloatValue(EffectRules.Shape.Arc_Default);
            Spread = new EffectShapeSpreadRandom();
        }
        public EffectShapeCircle(IFloat radius, IFloat aspect, IFloat thickness, IFloat arc, IEffectShapeSpread spread)
        {
            Radius = radius;
            Aspect = aspect;
            Thickness = thickness;
            Arc = arc;
            Spread = spread;
        }
        public void Reset()
        {
            Radius = new FloatValue(EffectRules.Shape.CircleRadius_Default);
            Aspect = new FloatValue(EffectRules.Shape.CircleAspect_Default);
            Thickness = new FloatValue(EffectRules.Shape.CircleThickness_Default);
            Arc = new FloatValue(EffectRules.Shape.Arc_Default);
            Spread = new EffectShapeSpreadRandom();
        }

        public object Clone() => Copy();
        IEffectShape ICopyable<IEffectShape>.Copy() => new EffectShapeCircle(Radius.Copy(), Aspect.Copy(), Thickness.Copy(), Arc.Copy(), Spread.Copy());
        public EffectShapeCircle Copy() => new(Radius.Copy(), Aspect.Copy(), Thickness.Copy(), Arc.Copy(), Spread.Copy());

        public override bool Equals(object obj) => obj is EffectShapeCircle value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(Radius, Aspect, Thickness, Arc, Spread);
        
        public bool Equals(IEffectShape other) => other is EffectShapeCircle value && Equals(value);
        public bool Equals(EffectShapeCircle other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Radius.Equals(other.Radius)
                         && Aspect.Equals(other.Aspect)
                         && Thickness.Equals(other.Thickness)
                         && Arc.Equals(other.Arc)
                         && Spread.Equals(other.Spread);
            return result;
        }
    }
}