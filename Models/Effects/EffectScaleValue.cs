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
    /// Every particle keeps one fixed size for its whole life - the simplest IEffectScale variant.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EffectScaleValue : IEffectScale, IModel<EffectScaleValue>
    {
        /// <summary> Particle size, per axis. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Scale)]
        public IVector2 Scale { get; set; }
        
        /// <summary> Which concrete form this is - the discriminator a converter writes and reads back. </summary>
        public EffectScaleType GetModelType() => EffectScaleType.Value;

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public EffectScaleValue()
        {
            Scale = new Vector2Value(
                EffectRules.Scale.A_X_Default, 
                EffectRules.Scale.A_Y_Default);
        }
        /// <summary> Built from its scale. </summary>
        public EffectScaleValue(IVector2 scale)
        {
            Scale = scale;
        }
    }
}