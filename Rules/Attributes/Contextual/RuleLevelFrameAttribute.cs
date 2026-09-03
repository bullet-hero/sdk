using System;
using System.Reflection;
using BH.SDK.Utils;

namespace BH.SDK.Rules.Attributes
{
    // Bounded by the timeline of the scope currently being walked, not by "the level's" - a frame
    // inside a prefab template belongs to that template's own FrameDuration, and measuring it against
    // the level's would let a 10-frame template hold a keyframe at frame 500.
    //
    // A root with no timeline at all (LevelMeta, UserSettings, a standalone value model) still gets
    // its lower bound checked. Reporting every frame as broken there - the old behaviour, which fell
    // out of requiring the context to literally be a Level - made the issue unfixable rather than
    // informative, since Fix had nothing to clamp against either.

    /// <summary>
    /// A frame must sit inside its own scope's timeline: [0, FrameDuration). The upper bound is
    /// exclusive - FrameDuration is a count, so the last playable frame is FrameDuration - 1.
    /// </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleLevelFrameAttribute : BasePropertyRuleAttribute
    {
        public override string RuleNameKey => "rule_level_frame";

        // Warning, not Error: a frame past its scope's timeline is not broken data, it is content that
        // never plays. The level runs, and what the author authored past the end simply does not
        // appear - which is exactly the difference this severity is for.
        public override RuleGroup Group => RuleGroup.Warning;

        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(int).IsAssignableFrom(property.PropertyType);

        protected override bool IsValidInternal(object value, RuleContext context)
        {
            if (value is not int frame || frame < FrameRules.MinFrame) return false;
            if (context is not { HasScope: true }) return true;

            return frame < context.FrameDuration;
        }

        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            if (property.GetValue(target) is not int frame) return;

            var hasTimeline = context is { HasScope: true }
                              && context.FrameDuration >= FrameRules.MinFrameDuration;
            var maxFrame = hasTimeline ? context.FrameDuration - 1 : int.MaxValue;

            if (frame < FrameRules.MinFrame || frame > maxFrame)
                property.SetValue(target, BHSDKMath.Clamp(frame, FrameRules.MinFrame, maxFrame));
        }
    }
}
