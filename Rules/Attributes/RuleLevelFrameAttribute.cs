using System;
using System.Reflection;

namespace BH.SDK.Rules.Attributes
{
    // ONE BOUND, AND IT IS THE LEFT ONE. A timeline's first frame is FrameRules.MinFrame and always
    // will be - it is the origin the whole format counts from - while its LAST frame is whatever the
    // author last dragged the level's end to. So a frame below the first one is a defect that nothing
    // can play (it is the value the runtime's own "Frame must be at least 1" assertion used to abort
    // a Burst batch over), and a frame past the last one is ordinary authored content sitting beyond
    // the current end: it plays the moment the level grows, and lengthening a level is a gesture, not
    // a repair.
    //
    // That is why this rule no longer measures against the scope's FrameDuration, which it did until
    // the right edge was recognised as movable. The old upper bound reported - and its Fix DESTROYED,
    // by clamping onto the last frame - every keyframe an author had parked past the end while
    // working, and every object left long by a level that had since been shortened.
    //
    // It therefore needs nothing from RuleContext any more, which is what took it out of
    // Attributes/Contextual/: the check is the same question everywhere, in a level, in a prefab
    // template, in a LevelMeta with no timeline at all.

    /// <summary>
    /// A frame must be on the timeline at all: <see cref="FrameRules.MinFrame"/> or later. The upper
    /// end is deliberately unbounded - a level's length moves, its origin does not.
    /// </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleLevelFrameAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_level_frame"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_level_frame";

        // Warning rather than Error, and the reason moved with the bound. It used to be "content past
        // the end never plays"; now it is that the repair is a single unambiguous clamp onto the first
        // frame, which loses whatever the author meant by the number - the level opens and plays
        // either way, with that one key held at the beginning.

        /// <summary> A warning: the level still plays, but this is not what the author meant. </summary>
        public override RuleGroup Group => RuleGroup.Warning;

        /// <summary> Applies to int properties. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(int).IsAssignableFrom(property.PropertyType);

        /// <summary> Passes for any frame on the timeline, <see cref="FrameRules.NoFrame"/> excluded. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
            => value is int frame && FrameRules.IsValidFrame(frame);

        /// <summary> Raises a frame below the first one onto it; leaves every other value alone. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            if (property.GetValue(target) is not int frame) return;
            if (FrameRules.IsValidFrame(frame)) return;

            property.SetValue(target, FrameRules.MinFrame);
        }
    }
}
