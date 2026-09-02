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
    /// Tint drawn per channel between two colors - any color inside the RGBA box, not just the line
    /// between A and B the uniform variant produces. Same fields, much wider spread.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EffectColorRandomPerComponent : IEffectColor, IModel<EffectColorRandomPerComponent>
    {
        /// <summary> Per-channel first bound. </summary>
        [RuleNotNull]
        [JsonProperty(Names.ColorA)]
        public IColor4 Color4A { get; set; }

        /// <summary> Per-channel second bound. </summary>
        [RuleNotNull]
        [JsonProperty(Names.ColorB)]
        public IColor4 Color4B { get; set; }
        
        public EffectColorType GetModelType() => EffectColorType.RandomPerComponent;

        public EffectColorRandomPerComponent()
        {
            Color4A = new Color4Value(
                EffectRules.Color.A_R_Default,
                EffectRules.Color.A_G_Default,
                EffectRules.Color.A_B_Default,
                EffectRules.Color.A_A_Default);
            Color4B = new Color4Value(
                EffectRules.Color.B_R_Default,
                EffectRules.Color.B_G_Default,
                EffectRules.Color.B_B_Default,
                EffectRules.Color.B_A_Default);
        }
        public EffectColorRandomPerComponent(IColor4 color4A, IColor4 color4B)
        {
            Color4A = color4A;
            Color4B = color4B;
        }
    }
}