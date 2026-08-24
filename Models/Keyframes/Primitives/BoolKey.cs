using System;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Keyframes
{
    /// <summary>
    /// An on/off switch at a frame - player visibility, controls, collisions. The one key type that
    /// does NOT derive from Keyframe: a toggle has nothing to interpolate, so it carries no EaseType.
    /// </summary>
    [RuleContainer]
    public class BoolKey : IFrame, IModel<BoolKey>
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
        public void Reset()
        {
            Frame = FrameRules.MinFrame;
            Value = false;
        }

        public object Clone() => Copy();
        public BoolKey Copy() => new(Value, Frame);

        public void Update(BoolKey src)
        {
            Value = src.Value;
            Frame = src.Frame;
        }

        public void Pull(BoolKey src)
        {
            Value = src.Value;
            Frame = src.Frame;
        }

        public override bool Equals(object obj) => obj is BoolKey value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(Frame, Value);

        public bool Equals(BoolKey other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Frame.Equals(other.Frame) && Value.Equals(other.Value);
            return result;
        }
    }
}