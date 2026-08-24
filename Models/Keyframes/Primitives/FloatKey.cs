using System;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Keyframes
{
    /// <summary>
    /// Generic single-number track entry - the key type post-processing and any unnamed scalar
    /// parameter animate through, where AngleKey/ZoomKey exist only because their meaning is fixed.
    /// </summary>
    [RuleContainer]
    public class FloatKey : Keyframe, IModel<FloatKey>
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
        public override void Reset()
        {
            base.Reset();
            Value = new FloatValue();
        }
        
        public override object Clone() => CopyImpl();
        public override Keyframe Copy() => CopyImpl();
        FloatKey ICopyable<FloatKey>.Copy() => CopyImpl();
        
        private FloatKey CopyImpl() => new(Value.Copy(), Frame, Ease);

        public void Update(FloatKey src)
        {
            base.Update(src);

            Value = src.Value.Copy();
        }

        public void Pull(FloatKey src)
        {
            base.Pull(src);

            Value = Value.PullFrom(src.Value);
        }

        public override bool Equals(object obj) => obj is FloatKey value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Value);

        public bool Equals(FloatKey other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other) && Value.Equals(other.Value);
            return result;
        }
    }
}