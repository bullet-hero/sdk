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
    /// Rotation drawn between two bounds with an independent draw per component. Same fields as
    /// EffectAngleRandomUniform; the split exists so the pair matches the scale/color families,
    /// where per-component really does produce a different look.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EffectAngleRandomPerComponent : IEffectAngle, IModel<EffectAngleRandomPerComponent>
    {
        /// <summary> First bound of the draw. </summary>
        [RuleNotNull]
        [JsonProperty(Names.AngleA)]
        public IFloat AngleA { get; set; }

        /// <summary> Second bound of the draw. </summary>
        [RuleNotNull]
        [JsonProperty(Names.AngleB)]
        public IFloat AngleB { get; set; }
        
        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public EffectAngleType GetModelType() => EffectAngleType.RandomPerComponent;

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public EffectAngleRandomPerComponent()
        {
            AngleA = new FloatValue(EffectRules.Angle.A_Default);
            AngleB = new FloatValue(EffectRules.Angle.B_Default);
        }
        /// <summary> Built from its A and B. </summary>
        public EffectAngleRandomPerComponent(float angleA, float angleB)
        {
            AngleA = new FloatValue(angleA);
            AngleB = new FloatValue(angleB);
        }
        /// <summary> Built from its A and B. </summary>
        public EffectAngleRandomPerComponent(IFloat angleA, IFloat angleB)
        {
            AngleA = angleA;
            AngleB = angleB;
        }
    }
}