using System;
using System.Collections.Generic;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Values
{
    /// <summary>
    /// A whole animation curve stored as data, used inside effects (particle angle/scale over life).
    /// Different axis from the level timeline: its Time is normalized 0..1 progress of a particle,
    /// not a level frame - which is why it needs its own wrap modes instead of a FrameSpan.
    /// </summary>
    [RuleContainer]
    public class CurveValue : IModel<CurveValue>
    {
        /// <summary> Control points of the curve, each with its own tangents. </summary>
        [RuleNotNull, RuleCollectionNoNullItems]
        [RuleCollectionMinCount(ValueRules.MinCurveKeys), RuleCollectionMaxCount(ValueRules.MaxCurveKeys)]
        [RuleCollectionSorted(nameof(CurveKeyframeValue.Time))]
        [JsonProperty(Names.Keys)]
        public List<CurveKeyframeValue> KeyFrames { get; set; }

        /// <summary> What the curve reads as before its first key (clamp, loop, ping-pong). </summary>
        [RuleEnumValid(CurveWrapMode.Default)]
        [JsonProperty(Names.PreWrapMode)]
        public CurveWrapMode PreWrapMode { get; set; }

        /// <summary> Same, after its last key - the two ends wrap independently. </summary>
        [RuleEnumValid(CurveWrapMode.Default)]
        [JsonProperty(Names.PostWrapMode)]
        public CurveWrapMode PostWrapMode { get; set; }
        
        public CurveValue()
        {
            KeyFrames = new List<CurveKeyframeValue>();
            PreWrapMode = CurveWrapMode.Default;
            PostWrapMode = CurveWrapMode.Default;
        }
        public CurveValue(List<CurveKeyframeValue> keyFrames, CurveWrapMode preWrapMode, CurveWrapMode postWrapMode)
        {
            KeyFrames = keyFrames;
            PreWrapMode = preWrapMode;
            PostWrapMode = postWrapMode;
        }
        public void Reset()
        {
            KeyFrames.Clear();
            PreWrapMode = CurveWrapMode.Default;
            PostWrapMode = CurveWrapMode.Default;
        }
        
        public object Clone() => Copy();
        public CurveValue Copy() => new(KeyFrames.CopyList(), PreWrapMode, PostWrapMode);

        public void Update(CurveValue src)
        {
            KeyFrames = src.KeyFrames.CopyList();
            PreWrapMode = src.PreWrapMode;
            PostWrapMode = src.PostWrapMode;
        }

        public void Pull(CurveValue src)
        {
            KeyFrames = src.KeyFrames.CopyList();
            PreWrapMode = src.PreWrapMode;
            PostWrapMode = src.PostWrapMode;
        }

        public override bool Equals(object obj) => obj is CurveValue value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(KeyFrames.GetListHashCode(), (int)PreWrapMode, (int)PostWrapMode);

        public bool Equals(CurveValue other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = KeyFrames.ListEquals(other.KeyFrames)
                         && PreWrapMode == other.PreWrapMode
                         && PostWrapMode == other.PostWrapMode;
            return result;
        }
    }
}