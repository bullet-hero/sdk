using System.Collections.Generic;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;

namespace BH.SDK.Utils
{
    // The ownership table for Prefab.Root, shared by the edit-time PrefabMaterializer and the load-time
    // Expand: the template owns Name, the seven tracks and the DURATION, the placement owns start, Active,
    // Layer and PlacementOffset. No frame offset on the root (Docs/PREFAB_PLACEMENT.md).
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

        // The root is not a key in ObjectIds, so a modification addressed at it skips the table lookup or it
        // is recorded, saved and never applied (MODIFICATION_FIELD_IDS_HISTORY.md).

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

        // ObjectIds is keyed inner -> outer; this reverse question is a scan (at most MaxObjectIdRemaps
        // entries).

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

        // The one writer of a template's length: start MinFrame, no anchors (RulePrefabRootFixed reports
        // both).

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
