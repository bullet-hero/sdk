using System;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;

namespace BH.SDK.Rules.Attributes
{
    // A class rule: the pinned fields live on RectObject, shared by every object. A placement owns
    // Active and Layer; PrefabRootUtils copies neither and ApplyRoot copies only the duration.
    // Why each field is pinned: BH.SDK/Docs/RULES.md, "RulePrefabRootFixed".

    /// <summary>
    /// A template's root is itself, always active, on layer zero, begins on the first frame with
    /// neither edge anchored: every field of <see cref="Prefab.Root"/> that belongs to a placement.
    /// </summary>
    [AttributeUsage(ClassTarget)]
    public class RulePrefabRootFixedAttribute : BaseObjectRuleAttribute
    {
        /// <summary> <c>"rule_prefab_root_fixed"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_prefab_root_fixed";

        /// <summary> A warning: a value here reaches nothing, so it misleads rather than breaks. </summary>
        public override RuleGroup Group => RuleGroup.Warning;

        /// <summary> Sits on the one type owning a template root. </summary>
        protected override bool IsValidTypeInternal(Type type)
            => typeof(Prefab).IsAssignableFrom(type);

        /// <summary> Passes when the root carries the root id, is active, is on layer zero and
        /// starts on the first frame unanchored. </summary>
        protected override bool IsValidInternal(object target, RuleContext context)
        {
            if (target is not Prefab prefab) return false;

            var root = prefab.Root;
            if (root == null) return false;

            return root.ObjectId == ObjectId.PrefabRoot
                   && root.Active
                   && root.Layer == ValueRules.DefaultLayer
                   && root.Span.StartFrame == FrameRules.MinFrame
                   && root.Span.Anchors == FrameAnchor.None;
        }

        // The DURATION is carried through rather than reset - it is the template's own length and
        // the only authored half of this span, so a fix that dropped it would shorten every
        // placement of the template to one frame.

        /// <summary> Writes every pinned field back to the only value anything reads. </summary>
        protected override void FixInternal(object target, RuleContext context)
        {
            if (target is not Prefab prefab || prefab.Root == null) return;

            var root = prefab.Root;
            root.ObjectId = ObjectId.PrefabRoot;
            root.Active = true;
            root.Layer = ValueRules.DefaultLayer;
            root.Span = new FrameSpan(FrameRules.MinFrame, root.Span.FrameDuration);
        }
    }
}
