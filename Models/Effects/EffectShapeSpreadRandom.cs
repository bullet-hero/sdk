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
    /// Each particle lands at a random point of the shape's arc/segment - scattered, with no
    /// relation between consecutive spawns.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EffectShapeSpreadRandom : IEffectShapeSpread, IModel<EffectShapeSpreadRandom>
    {
        /// <summary> How much of the shape the draw may cover; smaller values keep spawns clustered. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Spread)]
        public IFloat Spread { get; set; }
        
        public EffectShapeSpreadType GetModelType() => EffectShapeSpreadType.Random;
        
        public EffectShapeSpreadRandom()
        {
            Spread = new FloatValue(EffectRules.ShapeSpread.Spread_Default);
        }
        public EffectShapeSpreadRandom(IFloat spread)
        {
            Spread = spread;
        }
    }
}