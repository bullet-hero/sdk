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
    /// Tint blended between two colors by a single random factor - every particle lands somewhere on
    /// the straight line between A and B, so the palette stays coherent.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EffectColorRandomUniform : IEffectColor, IModel<EffectColorRandomUniform>
    {
        /// <summary> One end of the blend. </summary>
        [RuleNotNull]
        [JsonProperty(Names.ColorA)]
        public IColor4 Color4A { get; set; }

        /// <summary> The other end of the blend. </summary>
        [RuleNotNull]
        [JsonProperty(Names.ColorB)]
        public IColor4 Color4B { get; set; }
        
        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public EffectColorType GetModelType() => EffectColorType.RandomUniform;

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public EffectColorRandomUniform()
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
        /// <summary> Built from its 4 A and 4 B. </summary>
        public EffectColorRandomUniform(IColor4 color4A, IColor4 color4B)
        {
            Color4A = color4A;
            Color4B = color4B;
        }
    }
}