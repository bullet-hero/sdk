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
    /// Size drawn between two vectors with one shared factor, so a particle stays proportional -
    /// bigger or smaller, never stretched. That is the whole difference from RandomPerComponent.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EffectScaleRandomUniform : IEffectScale, IModel<EffectScaleRandomUniform>
    {
        /// <summary> One end of the size range (the JSON key reads "scale x" for historical
        /// reasons - it is the A bound, not the X axis). </summary>
        [RuleNotNull]
        [JsonProperty(Names.ScaleX)]
        public IVector2 ScaleA { get; set; }

        /// <summary> The other end of the size range. </summary>
        [RuleNotNull]
        [JsonProperty(Names.ScaleY)]
        public IVector2 ScaleB { get; set; }

        public EffectScaleType GetModelType() => EffectScaleType.RandomUniform;

        public EffectScaleRandomUniform()
        {
            ScaleA = new Vector2Value(
                EffectRules.Scale.A_X_Default, 
                EffectRules.Scale.A_Y_Default);
            ScaleB = new Vector2Value(
                EffectRules.Scale.B_X_Default, 
                EffectRules.Scale.B_Y_Default);
        }
        public EffectScaleRandomUniform(IVector2 scaleA, IVector2 scaleB)
        {
            ScaleA = scaleA;
            ScaleB = scaleB;
        }
    }
}