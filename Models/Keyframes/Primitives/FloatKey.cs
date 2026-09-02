using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.Keyframes
{
    /// <summary>
    /// Generic single-number track entry - the key type post-processing and any unnamed scalar
    /// parameter animate through, where AngleKey/ZoomKey exist only because their meaning is fixed.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class FloatKey : Keyframe, IModel<FloatKey>
    {
        /// <summary> Value at this frame. </summary>
        [RuleNotNull(typeof(FloatValue))]
        [JsonProperty(Names.Float)]
        public IFloat Value { get; set; }

        public FloatKey()
        {
            Value = new FloatValue();
        }
        public FloatKey(IFloat value, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Value = value;
        }
    }
}