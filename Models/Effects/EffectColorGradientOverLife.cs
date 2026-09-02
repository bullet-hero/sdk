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
        
        public EffectColorType GetModelType() => EffectColorType.GradientOverLife;

        public EffectColorGradientOverLife()
        {
            Gradient = EffectRules.GetGradient_Default();
        }
        public EffectColorGradientOverLife(GradientValue gradient)
        {
            Gradient = gradient;
        }
    }
}