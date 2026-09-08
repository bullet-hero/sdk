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
    /// Key of an anchor or pivot track. Same payload shape as Vector2Key, but clamped to 0..1 and
    /// defaulting to center - animating it moves the point an object rotates and scales around.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class AlignmentKey : Keyframe, IModel<AlignmentKey>
    {
        /// <summary> Normalized point in the rect at this frame: (0,0) left-bottom, (1,1) right-top. </summary>
        [RuleNotNull(typeof(Vector2Value)), RuleIVector2InRange(ValueRules.MinAlignment, ValueRules.MaxAlignment)]
        [JsonProperty(Names.Vector2)]
        public IVector2 Value { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public AlignmentKey()
        {
            Value = Alignment.DefaultValue;
        }
        /// <summary> Built from its value, frame and default ease. </summary>
        public AlignmentKey(IVector2 value, int frame, EaseType ease = DefaultEase) : base(frame, ease)
        {
            Value = value;
        }

        /// <summary> A keyframe pinning the pivot to the bottom of the left. </summary>
        public static AlignmentKey GetLeftBottom(int frame, EaseType ease = DefaultEase)
            => new(Alignment.LeftBottomValue, frame, ease);
        /// <summary> A keyframe pinning the pivot to the vertical centre of the left. </summary>
        public static AlignmentKey GetLeftMiddle(int frame, EaseType ease = DefaultEase)
            => new(Alignment.LeftMiddleValue, frame, ease);
        /// <summary> A keyframe pinning the pivot to the top of the left. </summary>
        public static AlignmentKey GetLeftTop(int frame, EaseType ease = DefaultEase)
            => new(Alignment.LeftTopValue, frame, ease);
        /// <summary> A keyframe pinning the pivot to the bottom of the horizontal centre. </summary>
        public static AlignmentKey GetCenterBottom(int frame, EaseType ease = DefaultEase)
            => new(Alignment.CenterBottomValue, frame, ease);
        /// <summary> A keyframe pinning the pivot to the vertical centre of the horizontal centre. </summary>
        public static AlignmentKey GetCenterMiddle(int frame, EaseType ease = DefaultEase)
            => new(Alignment.CenterMiddleValue, frame, ease);
        /// <summary> A keyframe pinning the pivot to the top of the horizontal centre. </summary>
        public static AlignmentKey GetCenterTop(int frame, EaseType ease = DefaultEase)
            => new(Alignment.CenterTopValue, frame, ease);
        /// <summary> A keyframe pinning the pivot to the bottom of the right. </summary>
        public static AlignmentKey GetRightBottom(int frame, EaseType ease = DefaultEase)
            => new(Alignment.RightBottomValue, frame, ease);
        /// <summary> A keyframe pinning the pivot to the vertical centre of the right. </summary>
        public static AlignmentKey GetRightMiddle(int frame, EaseType ease = DefaultEase)
            => new(Alignment.RightMiddleValue, frame, ease);
        /// <summary> A keyframe pinning the pivot to the top of the right. </summary>
        public static AlignmentKey GetRightTop(int frame, EaseType ease = DefaultEase)
            => new(Alignment.RightTopValue, frame, ease);
    }
}