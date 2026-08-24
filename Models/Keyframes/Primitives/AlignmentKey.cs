using System;
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
    /// Key of an anchor or pivot track. Same payload shape as Vector2Key, but clamped to 0..1 and
    /// defaulting to center - animating it moves the point an object rotates and scales around.
    /// </summary>
    [RuleContainer]
    public class AlignmentKey : Keyframe, IModel<AlignmentKey>
    {
        /// <summary> Normalized point in the rect at this frame: (0,0) left-bottom, (1,1) right-top. </summary>
        [RuleNotNull(typeof(Vector2Value)), RuleIVector2InRange(ValueRules.MinAlignment, ValueRules.MaxAlignment)]
        [JsonProperty(Names.Vector2)]
        public IVector2 Value { get; set; }

        public AlignmentKey()
        {
            Value = Alignment.DefaultValue;
        }
        public AlignmentKey(IVector2 value, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Value = value;
        }
        public override void Reset()
        {
            base.Reset();
            Value = Alignment.DefaultValue;
        }

        public override object Clone() => CopyImpl();
        public override Keyframe Copy() => CopyImpl();
        AlignmentKey ICopyable<AlignmentKey>.Copy() => CopyImpl();
        
        private AlignmentKey CopyImpl() => new(Value.Copy(), Frame, Ease);

        public void Update(AlignmentKey src)
        {
            base.Update(src);

            Value = src.Value.Copy();
        }

        public void Pull(AlignmentKey src)
        {
            base.Pull(src);

            Value = Value.PullFrom(src.Value);
        }

        public override bool Equals(object obj) => obj is AlignmentKey value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(Frame, Value);

        public bool Equals(AlignmentKey other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = base.Equals(other) && Value.Equals(other.Value);
            return result;
        }
        
        public static AlignmentKey GetLeftBottom(int frame, EaseType ease = DefaultEase)
            => new(Alignment.LeftBottomValue, frame, ease);
        public static AlignmentKey GetLeftMiddle(int frame, EaseType ease = DefaultEase)
            => new(Alignment.LeftMiddleValue, frame, ease);
        public static AlignmentKey GetLeftTop(int frame, EaseType ease = DefaultEase)
            => new(Alignment.LeftTopValue, frame, ease);
        public static AlignmentKey GetCenterBottom(int frame, EaseType ease = DefaultEase)
            => new(Alignment.CenterBottomValue, frame, ease);
        public static AlignmentKey GetCenterMiddle(int frame, EaseType ease = DefaultEase)
            => new(Alignment.CenterMiddleValue, frame, ease);
        public static AlignmentKey GetCenterTop(int frame, EaseType ease = DefaultEase)
            => new(Alignment.CenterTopValue, frame, ease);
        public static AlignmentKey GetRightBottom(int frame, EaseType ease = DefaultEase)
            => new(Alignment.RightBottomValue, frame, ease);
        public static AlignmentKey GetRightMiddle(int frame, EaseType ease = DefaultEase)
            => new(Alignment.RightMiddleValue, frame, ease);
        public static AlignmentKey GetRightTop(int frame, EaseType ease = DefaultEase)
            => new(Alignment.RightTopValue, frame, ease);
    }
}