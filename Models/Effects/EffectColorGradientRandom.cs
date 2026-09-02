using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums.Effects;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Effects;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Effects
{
    /// <summary>
    /// Each particle picks one random spot on the gradient and keeps that color - a palette to draw
    /// from, not an animation. The gradient is a color set here, unlike the OverLife/BySpeed variants
    /// where it is a curve in time or speed.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EffectColorGradientRandom : IEffectColor, IModel<EffectColorGradientRandom>
    {
        /// <summary> Ramp the per-particle color is drawn from. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Gradient)]
        public GradientValue Gradient { get; set; }

        public EffectColorType GetModelType() => EffectColorType.GradientRandom;

        public EffectColorGradientRandom()
        {
            Gradient = EffectRules.GetGradient_Default();
        }
        public EffectColorGradientRandom(GradientValue gradient)
        {
            Gradient = gradient;
        }
    }
}