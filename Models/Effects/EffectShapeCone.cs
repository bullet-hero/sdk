using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Effects;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Effects;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Effects
{
    /// <summary>
    /// Emitter shape spawning particles in a widening (or narrowing) cone - a directional spray,
    /// where a circle would be omnidirectional.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EffectShapeCone : IEffectShape, IModel<EffectShapeCone>
    {
        /// <summary> Radius at the far end. Larger than BaseRadius flares out, smaller funnels in. </summary>
        [RuleNotNull, RuleIFloatMin(EffectRules.Shape.ConeTopRadius_Min)]
        [JsonProperty(Names.TopRadius)]
        public IFloat TopRadius { get; set; }

        /// <summary> Radius at the emitter end. </summary>
        [RuleNotNull, RuleIFloatMin(EffectRules.Shape.ConeBaseRadius_Min)]
        [JsonProperty(Names.BaseRadius)]
        public IFloat BaseRadius { get; set; }

        // FLATTENED, so its arc is a FAN rather than a sector: a 2D scene keeps one of the two axes
        // the cone sweeps and throws the other away, which makes the arc sweep the fan from one
        // edge to the other instead of going round. A full turn is the whole fan either way; a
        // partial one is what the phase decides, and it starts at +X like the other two shapes.

        /// <summary> Portion of the cone's circumference used, in radians, measured
        /// counter-clockwise from the +X axis - the same convention
        /// <see cref="EffectShapeCircle"/> and <see cref="EffectShapeTorus"/> follow. </summary>
        [RuleNotNull, RuleIFloatInRange(EffectRules.Shape.Arc_Min, EffectRules.Shape.Arc_Max)]
        [JsonProperty(Names.Arc)]
        public IFloat Arc { get; set; }

        /// <summary> Distance between the two ends - how deep the spawn volume is. </summary>
        [RuleNotNull, RuleIFloatMin(EffectRules.Shape.ConeHeight_Min)]
        [JsonProperty(Names.Height)]
        public IFloat Height { get; set; }

        /// <summary> How successive particles walk the arc. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Spread)]
        public IEffectShapeSpread Spread { get; set; }
        
        public EffectShapeType GetModelType() => EffectShapeType.Cone;
        
        public EffectShapeCone()
        {
            TopRadius = new FloatValue(EffectRules.Shape.ConeTopRadius_Default);
            BaseRadius = new FloatValue(EffectRules.Shape.ConeBaseRadius_Default);
            Arc = new FloatValue(EffectRules.Shape.Arc_Default);
            Height = new FloatValue(EffectRules.Shape.ConeHeight_Default);
            Spread = new EffectShapeSpreadRandom();
        }
        public EffectShapeCone(IFloat topRadius, IFloat baseRadius, IFloat arc, IFloat height, IEffectShapeSpread spread)
        {
            TopRadius = topRadius;
            BaseRadius = baseRadius;
            Arc = arc;
            Height = height;
            Spread = spread;
        }
    }
}