using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Keyframes
{
    /// <summary>
    /// Animated draw order. Interpolation is meaningless here (a layer is a discrete slot), so its
    /// Ease effectively picks when the swap happens rather than blending toward it.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class LayerKey : Keyframe, IModel<LayerKey>
    {
        /// <summary> Draw order from this frame on - higher draws in front. </summary>
        [RuleNotNull(typeof(IntValue)), RuleIIntInRange(ValueRules.MinLayer, ValueRules.MaxLayer)]
        [JsonProperty(Names.Int)]
        public IInt Layer { get; set; }

        public LayerKey()
        {
            Layer = new IntValue();
        }
        public LayerKey(IInt value, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Layer = value;
        }
    }
}