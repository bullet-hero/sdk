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
    /// Emitter shape spawning particles in a tube bent into a ring. Differs from Circle by having
    /// thickness as a real radius rather than an inward fill fraction.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EffectShapeTorus : IEffectShape, IModel<EffectShapeTorus>
    {
        /// <summary> Radius of the tube itself - how thick the ring is. </summary>
        [RuleNotNull, RuleIFloatMin(EffectRules.Shape.TorusRadiusMinor_Min)]
        [JsonProperty(Names.RadiusMinor)]
        public IFloat RadiusMinor { get; set; }

        /// <summary> Radius of the ring the tube is bent around. </summary>
        [RuleNotNull, RuleIFloatMin(EffectRules.Shape.TorusRadiusMajor_Min)]
        [JsonProperty(Names.RadiusMajor)]
        public IFloat RadiusMajor { get; set; }

        /// <summary> Portion of the ring used, in radians, measured counter-clockwise from the +X
        /// axis - the same convention <see cref="EffectShapeCircle"/> and
        /// <see cref="EffectShapeCone"/> follow. </summary>
        [RuleNotNull, RuleIFloatInRange(EffectRules.Shape.Arc_Min, EffectRules.Shape.Arc_Max)]
        [JsonProperty(Names.Arc)]
        public IFloat Arc { get; set; }

        /// <summary> How successive particles walk the arc. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Spread)]
        public IEffectShapeSpread Spread { get; set; }
        
        public EffectShapeType GetModelType() => EffectShapeType.Torus;
        
        public EffectShapeTorus()
        {
            RadiusMinor = new FloatValue(EffectRules.Shape.TorusRadiusMinor_Default);
            RadiusMajor = new FloatValue(EffectRules.Shape.TorusRadiusMajor_Default);
            Arc = new FloatValue(EffectRules.Shape.Arc_Default);
            Spread = new EffectShapeSpreadRandom();
        }
        public EffectShapeTorus(float radiusMinor, float radiusMajor, float arc, IEffectShapeSpread spread)
        {
            RadiusMinor = new FloatValue(radiusMinor);
            RadiusMajor = new FloatValue(radiusMajor);
            Arc = new FloatValue(arc);
            Spread = spread;
        }
        public EffectShapeTorus(IFloat radiusMinor, IFloat radiusMajor, IFloat arc, IEffectShapeSpread spread)
        {
            RadiusMinor = radiusMinor;
            RadiusMajor = radiusMajor;
            Arc = arc;
            Spread = spread;
        }
    }
}