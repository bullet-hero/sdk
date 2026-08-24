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
    /// Generic whole-number track entry, for counts and discrete modes that must not land between
    /// two values the way a FloatKey would.
    /// </summary>
    [RuleContainer]
    public class IntKey : Keyframe, IModel<IntKey>
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
        public override void Reset()
        {
            base.Reset();
            Value = new IntValue();
        }
        
        public override object Clone() => CopyImpl();
        public override Keyframe Copy() => CopyImpl();
        IntKey ICopyable<IntKey>.Copy() => CopyImpl();
        
        private IntKey CopyImpl() => new(Value.Copy(), Frame, Ease);

        public void Update(IntKey src)
        {
            base.Update(src);

            Value = src.Value.Copy();
        }

        public void Pull(IntKey src)
        {
            base.Pull(src);

            Value = Value.PullFrom(src.Value);
        }

        public override bool Equals(object obj) => obj is IntKey value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Value);

        public bool Equals(IntKey other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other) && Value.Equals(other.Value);
            return result;
        }
    }
}