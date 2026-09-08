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
    /// Tint read off a gradient by how fast the particle moves - heat-map style coloring where fast
    /// debris reads differently from settling debris.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EffectColorGradientBySpeed : IEffectColor, IModel<EffectColorGradientBySpeed>
    {
        /// <summary> Ramp sampled at normalized speed. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Gradient)]
        public GradientValue Gradient { get; set; }

        /// <summary> Speed window mapped onto the ramp's 0..1 axis; outside it the ends clamp. </summary>
        [RuleNotNull, RuleIVector2Ordered]
        [RuleIVector2InRange(EffectRules.SpeedRange_Min, EffectRules.SpeedRange_Max)]
        [JsonProperty(Names.SpeedRange)]
        public IVector2 SpeedRange { get; set; }

        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public EffectColorType GetModelType() => EffectColorType.GradientBySpeed;

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public EffectColorGradientBySpeed()
        {
            Gradient = EffectRules.GetGradient_Default();
            SpeedRange = new Vector2Value(
                EffectRules.Color.BySpeedRange_X_Default,
                EffectRules.Color.BySpeedRange_Y_Default);
        }
        /// <summary> Built from its gradient and range. </summary>
        public EffectColorGradientBySpeed(GradientValue gradient, IVector2 speedRange)
        {
            Gradient = gradient;
            SpeedRange = speedRange;
        }
    }
}