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
    /// Spawn point walks the shape in one direction and jumps back at the end - the classic rotating
    /// emitter that always turns the same way.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class EffectShapeSpreadLoop : IEffectShapeSpread, IModel<EffectShapeSpreadLoop>
    {
        /// <summary> Portion of the shape the walk covers before wrapping. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Spread)]
        public IFloat Spread { get; set; }

        /// <summary> How fast the spawn point advances - what sets the visible rotation rate. </summary>
        [RuleNotNull]
        [JsonProperty(Names.Speed)]
        public IFloat Speed { get; set; }
        
        public EffectShapeSpreadType GetModelType() => EffectShapeSpreadType.Loop;

        public EffectShapeSpreadLoop()
        {
            Spread = new FloatValue(EffectRules.ShapeSpread.Spread_Default);
            Speed = new FloatValue(EffectRules.ShapeSpread.Speed_Default);
        }
        public EffectShapeSpreadLoop(float spread, float speed)
        {
            Spread = new FloatValue(spread);
            Speed = new FloatValue(speed);
        }
        public EffectShapeSpreadLoop(IFloat spread, IFloat speed)
        {
            Spread = spread;
            Speed = speed;
        }
    }
}