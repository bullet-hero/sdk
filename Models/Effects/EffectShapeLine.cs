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
    /// Emitter shape spawning particles along a segment - walls and curtains of bullets, where the
    /// Spread decides whether the wall fills evenly, sweeps, or scatters.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EffectShapeLine : IEffectShape, IModel<EffectShapeLine>
    {
        /// <summary> One end of the segment, local to the effect object. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Start)]
        public IVector2 Start { get; set; }

        /// <summary> The other end of the segment. </summary>
        [RuleNotNull]
        [JsonProperty(Names.End)]
        public IVector2 End { get; set; }

        /// <summary> How successive particles walk from Start to End. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Spread)]
        public IEffectShapeSpread Spread { get; set; }
        
        public EffectShapeType GetModelType() => EffectShapeType.Line;
        
        public EffectShapeLine()
        {
            Start = new Vector2Value(
                EffectRules.Shape.LineStart_X_Default,
                EffectRules.Shape.LineStart_Y_Default);
            End = new Vector2Value(
                EffectRules.Shape.LineEnd_X_Default,
                EffectRules.Shape.LineEnd_Y_Default);
            Spread = new EffectShapeSpreadRandom();
        }
        public EffectShapeLine(IVector2 start, IVector2 end, IEffectShapeSpread spread)
        {
            Start = start;
            End = end;
            Spread = spread;
        }
    }
}