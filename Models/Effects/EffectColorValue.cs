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
    /// One constant tint for every particle - the simplest IEffectColor variant.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EffectColorValue : IEffectColor, IModel<EffectColorValue>
    {
        /// <summary> Particle tint. Polymorphic, so it can be a ThemeRef and follow the level's
        /// active palette. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Color)]
        public IColor4 Color4 { get; set; }
        
        public EffectColorType GetModelType() => EffectColorType.Value;

        public EffectColorValue()
        {
            Color4 = new Color4Value(
                EffectRules.Color.A_R_Default,
                EffectRules.Color.A_G_Default,
                EffectRules.Color.A_B_Default,
                EffectRules.Color.A_A_Default);
        }
        public EffectColorValue(IColor4 color4)
        {
            Color4 = color4;
        }
    }
}