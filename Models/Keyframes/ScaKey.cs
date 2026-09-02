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
    /// Two-axis multiplier key, used for both RectObject.Scales and RectObject.Sizes - the same shape
    /// serves "stretch relative to parent" and "how big the rect is" tracks.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class ScaKey : Keyframe, IModel<ScaKey>
    {
        /// <summary> Target scale/size at this frame; X and Y are independent, so non-uniform
        /// stretching is expressible. </summary>
        [RuleNotNull(typeof(Vector2Value)), RuleIVector2InRange(ValueRules.MinSca, ValueRules.MaxSca)]
        [JsonProperty(Names.Vector2)]
        public IVector2 Scale { get; set; }
        
        public ScaKey()
        {
            Scale = new Vector2Value();
        }
        public ScaKey(IVector2 scale, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Scale = scale;
        }
    }
}