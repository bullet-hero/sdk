using System;
using System.Reflection;

namespace BH.SDK.Rules.Attributes
{
    // ONE BOUND, THE LEFT ONE (FrameRules.MinFrame): the level's end is a number the author drags, so
    // a frame past it is content waiting for the level to grow. Needs nothing from RuleContext.
    // Why: BH.SDK/Docs/RULES.md, "RuleLevelFrame".

    /// <summary>
    /// A frame must be on the timeline at all: <see cref="FrameRules.MinFrame"/> or later. The upper
    /// end is deliberately unbounded - a level's length moves, its origin does not.
    /// </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleLevelFrameAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_level_frame"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_level_frame";

        // Warning: the only repair is a clamp onto the first frame, which loses what the author meant.

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
