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
    /// Generic whole-number track entry, for counts and discrete modes that must not land between
    /// two values the way a FloatKey would.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class IntKey : Keyframe, IModel<IntKey>
    {
        /// <summary> Value at this frame. </summary>
        [RuleNotNull(typeof(IntValue))]
        [JsonProperty(Names.Int)]
        public IInt Value { get; set; }

        public IntKey()
        {
            Value = new IntValue();
        }
        public IntKey(IInt value, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Value = value;
        }
    }
}