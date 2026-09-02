using System;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

namespace BH.SDK.Models.Keyframes
{
    /// <summary>
    /// An on/off switch at a frame - player visibility, controls, collisions. The one key type that
    /// does NOT derive from Keyframe: a toggle has nothing to interpolate, so it carries no EaseType.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class BoolKey : IFrame, IModel<BoolKey>
    {
        /// <summary> Level frame the switch flips on. </summary>
        [RuleLevelFrame]
        [JsonProperty(Names.FrameShort)]
        public int Frame { get; set; }

        /// <summary> State from this frame until the next key of the track. </summary>
        [JsonProperty(Names.Bool)]
        public bool Value { get; set; }

        public BoolKey()
        {
            Frame = FrameRules.MinFrame;
            Value = false;
        }
        public BoolKey(bool value, int frame)
        {
            Frame = frame;
            Value = value;
        }
    }
}