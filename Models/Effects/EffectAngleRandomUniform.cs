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
    /// Rotation drawn between two bounds, one draw shared by the whole particle. Structurally
    /// identical to EffectAngleRandomPerComponent - only the evaluation differs.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EffectAngleRandomUniform : IEffectAngle, IModel<EffectAngleRandomUniform>
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
        public EffectAngleType GetModelType() => EffectAngleType.RandomUniform;

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public EffectAngleRandomUniform()
        {
            AngleA = new FloatValue(EffectRules.Angle.A_Default);
            AngleB = new FloatValue(EffectRules.Angle.B_Default);
        }
        /// <summary> Built from its A and B. </summary>
        public EffectAngleRandomUniform(float angleA, float angleB)
        {
            AngleA = new FloatValue(angleA);
            AngleB = new FloatValue(angleB);
        }
        /// <summary> Built from its A and B. </summary>
        public EffectAngleRandomUniform(IFloat angleA, IFloat angleB)
        {
            AngleA = angleA;
            AngleB = angleB;
        }
    }
}