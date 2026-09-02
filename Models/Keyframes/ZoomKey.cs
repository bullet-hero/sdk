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
    /// Camera zoom key. The camera's replacement for a two-axis size track: zoom is one number
    /// because stretching the view per axis would break the level's aspect contract.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class ZoomKey : Keyframe, IModel<ZoomKey>
    {
        /// <summary> Visible-area multiplier at this frame - smaller means closer in. </summary>
        [RuleNotNull(typeof(FloatValue)), RuleIFloatInRange(ValueRules.MinZoom, ValueRules.MaxZoom)]
        [JsonProperty(Names.Float)]
        public IFloat Zoom { get; set; }

        public ZoomKey()
        {
            Zoom = new FloatValue(ValueRules.DefaultZoom);
        }
        public ZoomKey(IFloat zoom, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Zoom = zoom;
        }
    }
}