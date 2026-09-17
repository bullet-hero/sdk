using System.Collections.Generic;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;

namespace BH.SDK.Utils
{
    // THE OWNERSHIP TABLE FOR Prefab.Root, AS CODE, and it lives here rather than in either consumer
    // because there are TWO of them and they only stay correct together: the edit-time
    // PrefabMaterializer (Materialize/Resync) and the load-time PrefabVirtualizationUtils.Expand.
    // PrefabMaterializer's own header explains that pairing; this is the third rule they now share,
    // and the SDK is the only place both can reach (Core references the SDK, never the reverse).
    //
    // WHAT THE TEMPLATE OWNS: the root's Name, all seven positional tracks, and its span's
    // DURATION. Every one of them is written onto the placement on every materialize and every
    // resync, so editing the root in Prefab Mode moves and re-lengthens every placement of that
    // template.
    //
    // THE DURATION IS THE TEMPLATE'S BY DEFAULT AND OVERRIDABLE ON TOP, which is why ApplyRoot
    // still writes it unconditionally below: ApplyModifications runs AFTER this in both consumers,
    // so a placement that has diverged (ModificationFields.PlacementDuration, root-keyed) gets its
    // own length back a step later, and one that has not follows its template. A template has
    // exactly one timeline (Prefab.Root.Span) and a placement is that root materialized; what the
    // author trims is a divergence from it, recorded by OpLevelObjectSpan only when the written
    // duration actually differs. ModificationFields.Span stays out of IsPrefabRootField - a
    // whole-span override would restate the start and beat the next ordinary move.
    //
    // WHAT THE PLACEMENT OWNS: the span's START, its Active, its Layer and its PlacementOffset -
    // how far into the template it starts playing. Where a placement plays is the author's answer
    // per placement (the start and the in-point together are what ApplyPlacementFrameOffset reads
    // as the origin for everything inside it), and so are Active and Layer - the template's own are
    // pinned by RulePrefabRootFixed precisely because nothing copies them.
    //
    // Every positional track going to the TEMPLATE is a deliberate departure from Unity, which
    // treats an instance's root transform as per-instance and does not even record it as an
    // override. Here the opposite is required: a PrefabObject frequently has no parent at all, or is
    // pinned where its TRS keyframes ARE the content, so a template that dropped them would lose
    // authored information every time one was made. Divergence between two placements is expressed
    // as Modifications, which is what they are for.
    //
    // NO FRAME OFFSET IS APPLIED TO THE ROOT, and that asymmetry is worth stating because it is the
    // exact shape of the bug ApplyPlacementFrameOffset's own header is about: an inner object's span
    // is local to the template's timeline and has the placement's start added back, while the root
    // has no span anybody reads (see Prefab.Root) and the placement's own span is the anchor being
    // added. Shifting anything here would be counting that origin twice.
    public static class PrefabRootUtils
    {
        /// <summary> Writes the template-owned half of <see cref="Prefab.Root"/> onto the placement
        /// that materializes it. Leaves the span's start, Active and Layer alone - those are the
        /// placement's. </summary>
        public static void ApplyRoot(PrefabObject placement, Prefab template)
        {
            var root = template?.Root;
            if (placement == null || root == null) return;

            placement.Name = root.Name;
            // The start and the anchors stay the placement's; only the length comes from the root.
            placement.Span = placement.Span.WithDuration(root.Span.FrameDuration);

            placement.Positions = Copy(root.Positions);
            placement.Rotations = Copy(root.Rotations);
            placement.Scales = Copy(root.Scales);
            placement.Sizes = Copy(root.Sizes);
            placement.AnchorsMin = Copy(root.AnchorsMin);
            placement.AnchorsMax = Copy(root.AnchorsMax);
            placement.Pivots = Copy(root.Pivots);
        }

        // The root is not a key in placement.ObjectIds and never will be - it has no outer copy,
        // being the placement itself - so a modification addressed at it has to short-circuit the
        // table lookup rather than fail it. Without this branch an override on the root is recorded,
        // saved, listed as present and silently never applied, which is exactly the defect
        // Docs/Issues/MODIFICATION_FIELD_IDS_HISTORY.md is the record of.

        /// <summary> The object a <see cref="ModificationKey"/>'s inner id names on this placement -
        /// the placement itself for <see cref="ObjectId.PrefabRoot"/>, one of its materialized
        /// copies otherwise. </summary>
        public static bool TryGetModificationTarget(PrefabObject placement, IObjectScope hostScope,
            ObjectId innerId, out RectObject target)
        {
            target = null;
            if (placement == null || hostScope == null) return false;

            if (innerId == ObjectId.PrefabRoot)
            {
                target = placement;
                return true;
            }

            return placement.ObjectIds != null
                   && placement.ObjectIds.TryGetValue(innerId, out var outerId)
                   && hostScope.Objects.TryGetValue(outerId, out target);
        }

        // PrefabObject.ObjectIds is keyed inner -> outer because that is the direction every
        // materialize walks; this is the one question asked the other way, and it is a scan because
        // a placement holds at most PrefabRules.MaxObjectIdRemaps entries and a second dictionary
        // would have to be kept in step through every resync.

        /// <summary> The template-inner id a placement materialized as <paramref name="outerId"/>. </summary>
        public static bool TryGetInnerId(PrefabObject placement, ObjectId outerId, out ObjectId innerId)
        {
            innerId = ObjectId.Null;
            if (placement?.ObjectIds == null) return false;

            foreach (var pair in placement.ObjectIds)
            {
                if (pair.Value != outerId) continue;
                innerId = pair.Key;
                return true;
            }

            return false;
        }

        // THE ONE WRITER OF A TEMPLATE'S LENGTH, and it exists so that the pinned half of that span
        // is spelled once. Every caller has a duration and nothing else to say: the start is
        // FrameRules.MinFrame because the timeline counts from one, and the anchors are none because
        // the root has no parent to follow (RulePrefabRootFixed reports both).

        /// <summary> Sets the template's own timeline length, the authored half of
        /// <see cref="Prefab.Root"/>'s span. </summary>
        public static void SetTemplateLength(Prefab template, int frameDuration)
        {
            if (template?.Root == null) return;
            template.Root.Span = new FrameSpan(FrameRules.MinFrame, frameDuration);
        }

        private static List<T> Copy<T>(List<T> list) where T : Keyframe, ICopyable<T>
            => list == null ? new List<T>() : list.CopyList();
    }
}
