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
        [JsonProperty(Names.MinorRadius)]
        public IFloat MinorRadius { get; set; }

        /// <summary> Radius of the ring the tube is bent around. </summary>
        [RuleNotNull, RuleIFloatMin(EffectRules.Shape.TorusRadiusMajor_Min)]
        [JsonProperty(Names.MajorRadius)]
        public IFloat MajorRadius { get; set; }

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
        
        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public EffectShapeType GetModelType() => EffectShapeType.Torus;
        
        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public EffectShapeTorus()
        {
            MinorRadius = new FloatValue(EffectRules.Shape.TorusRadiusMinor_Default);
            MajorRadius = new FloatValue(EffectRules.Shape.TorusRadiusMajor_Default);
            Arc = new FloatValue(EffectRules.Shape.Arc_Default);
            Spread = new EffectShapeSpreadRandom();
        }
        /// <summary> Built from its minor, major, arc and spread. </summary>
        public EffectShapeTorus(float radiusMinor, float radiusMajor, float arc, IEffectShapeSpread spread)
        {
            MinorRadius = new FloatValue(radiusMinor);
            MajorRadius = new FloatValue(radiusMajor);
            Arc = new FloatValue(arc);
            Spread = spread;
        }
        /// <summary> Built from its minor, major, arc and spread. </summary>
        public EffectShapeTorus(IFloat radiusMinor, IFloat radiusMajor, IFloat arc, IEffectShapeSpread spread)
        {
            MinorRadius = radiusMinor;
            MajorRadius = radiusMajor;
            Arc = arc;
            Spread = spread;
        }
    }
}