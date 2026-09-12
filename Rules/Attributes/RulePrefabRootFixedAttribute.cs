using System;
using BH.SDK.Models.Enums;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;

namespace BH.SDK.Rules.Attributes
{
    // A class rule rather than two property ones, and not by preference: the fields it pins live on
    // RectObject, shared by every object in the format, so a property rule would have to be true of
    // every object rather than of this one slot. Prefab.Root is the only RectObject in the model
    // whose Active and Layer are not the author's to set.
    //
    // WHY THEY ARE PINNED AT ALL: a placement owns both. Active gates rendering and collision down
    // the whole hierarchy and a template that could switch its own root off would switch off every
    // placement of itself; Layer is parent-relative and summed up the chain, so a non-zero one on
    // the root would shift every placement's draw order by a number the author set somewhere else
    // entirely. PrefabRootUtils never copies either, so a value here reaches nothing - which is
    // exactly why it is worth reporting rather than tolerating.
    //
    // THE IDENTITY IS HERE rather than left to RuleObjectIdValid, because that rule can only say
    // "PrefabRoot is legal as an identity inside a template" - it sees one value and no path, so it
    // cannot say "and this particular slot must hold exactly that". This can.
    //
    // ROOT.SPAN IS HALF PINNED AND HALF AUTHORED, which is why it is checked here rather than left
    // to a property rule on RectObject.Span. Its DURATION is the template's whole timeline and the
    // one number an author sets; its START is FrameRules.MinFrame because the timeline counts from
    // one and a template has nothing to start later than, and its ANCHORS are none because an
    // anchor means "this edge follows the parent's" and the root has no parent inside the template.
    // A start of 40 here would read as "the template begins on frame 40", which nothing implements:
    // PrefabRootUtils.ApplyRoot copies the duration and nothing else, so the number would reach no
    // placement at all.

    /// <summary>
    /// A template's root is itself, is always active, is always on layer zero, and begins on the
    /// first frame with neither edge anchored - its identity plus every field of
    /// <see cref="Prefab.Root"/> that belongs to a placement rather than to the template.
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
