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
    /// Size drawn per axis, so particles come out stretched in random directions - the deliberately
    /// non-proportional twin of EffectScaleRandomUniform.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EffectScaleRandomPerComponent : IEffectScale, IModel<EffectScaleRandomPerComponent>
    {
        /// <summary> Per-axis first bound of the size draw. </summary>
        [RuleNotNull]
        [JsonProperty(Names.ScaleX)]
        public IVector2 ScaleA { get; set; }

        /// <summary> Per-axis second bound of the size draw. </summary>
        [RuleNotNull]
        [JsonProperty(Names.ScaleY)]
        public IVector2 ScaleB { get; set; }
        
        public EffectScaleType GetModelType() => EffectScaleType.RandomPerComponent;

        public EffectScaleRandomPerComponent()
        {
            ScaleA = new Vector2Value(
                EffectRules.Scale.A_X_Default, 
                EffectRules.Scale.A_Y_Default);
            ScaleB = new Vector2Value(
                EffectRules.Scale.B_X_Default, 
                EffectRules.Scale.B_Y_Default);
        }
        public EffectScaleRandomPerComponent(IVector2 scaleA, IVector2 scaleB)
        {
            ScaleA = scaleA;
            ScaleB = scaleB;
        }
    }
}