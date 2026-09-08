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
    /// Tint read off a gradient by the particle's age - the usual way to fade a particle out, since
    /// the gradient's alpha track handles the fade without touching the color.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EffectColorGradientOverLife : IEffectColor, IModel<EffectColorGradientOverLife>
    {
        /// <summary> Ramp sampled at normalized lifetime (0 = spawn, 1 = death). </summary>
        [RuleNotNull]
        [JsonProperty(Names.Gradient)]
        public GradientValue Gradient { get; set; }
        
        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public EffectColorType GetModelType() => EffectColorType.GradientOverLife;

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public EffectColorGradientOverLife()
        {
            Gradient = EffectRules.GetGradient_Default();
        }
        /// <summary> Built from its gradient. </summary>
        public EffectColorGradientOverLife(GradientValue gradient)
        {
            Gradient = gradient;
        }
    }
}