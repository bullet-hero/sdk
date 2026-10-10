using System.Collections.Generic;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;

namespace BH.SDK.Utils
{
    // Flattening drops the placement bookkeeping and keeps the copies. It must answer for EVERY nested
    // PrefabObject: flatten the whole chain, or the top alone with the nested table REBASED first
    // (Docs/PREFAB_PLACEMENT.md).

    /// <summary>
    /// Turning prefab placements back into ordinary objects: which objects one flatten has to touch,
    /// and what a flattened placement becomes.
    /// </summary>
    public static class PrefabFlattenUtils
    {
        /// <summary> One placement plus every PrefabObject it materialized, transitively - the exact
        /// set that has to stop being placements together. Appends to <paramref name="into"/>. </summary>
        public static void CollectChain(IObjectScope scope, PrefabObject placement, List<ObjectId> into)
        {
            if (scope?.Objects == null || placement == null || into == null) return;


            var seen = new HashSet<ObjectId>();
            var pending = new Queue<PrefabObject>();
            pending.Enqueue(placement);
            seen.Add(placement.ObjectId);

            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                into.Add(current.ObjectId);

                if (current.ObjectIds == null) continue;
                foreach (var outerId in current.ObjectIds.Values)
                {
                    if (!seen.Add(outerId)) continue;
                    if (scope.Objects.TryGetValue(outerId, out var child) && child is PrefabObject nested)
                        pending.Enqueue(nested);
                }
            }
        }

        /// <summary> Every PrefabObject this placement materialized DIRECTLY - the copies a top-only
        /// flatten has to rebase instead of flattening. Appends to <paramref name="into"/>. </summary>
        public static void CollectDirectNested(IObjectScope scope, PrefabObject placement, List<PrefabObject> into)
        {
            if (scope?.Objects == null || placement?.ObjectIds == null || into == null) return;

            foreach (var outerId in placement.ObjectIds.Values)
                if (scope.Objects.TryGetValue(outerId, out var child) && child is PrefabObject nested)
                    into.Add(nested);
        }

        /// <summary> Moves a nested copy's ObjectIds out of the owner's template scope and into the
        /// host's, making it a genuine placement there. False changes NOTHING - an entry the owner
        /// cannot name means this copy has to be flattened rather than kept. </summary>
        public static bool TryRebaseNested(PrefabObject nested, PrefabObject owner)
        {
            if (nested?.ObjectIds == null || owner?.ObjectIds == null) return false;

            // Resolved in full before anything is written: a table half in one scope and half in
            // another names real host objects for some of its children and strangers for the rest,
            // which no later pass can tell from a correct one.
            var innerIds = new List<ObjectId>(nested.ObjectIds.Keys);
            var hostIds = new List<ObjectId>(innerIds.Count);
            foreach (var innerId in innerIds)
            {
                if (!owner.ObjectIds.TryGetValue(nested.ObjectIds[innerId], out var hostId)) return false;
                hostIds.Add(hostId);
            }

            for (var i = 0; i < innerIds.Count; i++)
                nested.ObjectIds[innerIds[i]] = hostIds[i];
            return true;
        }

        // Deliberately takes every PrefabObject in the scope; it does not draw FindPlacements' copy
        // distinction.

        /// <summary> Every PrefabObject in a scope - what a whole-scope flatten has to touch.
        /// Appends to <paramref name="into"/>. </summary>
        public static void CollectAll(IObjectScope scope, List<ObjectId> into)
        {
            if (scope?.Objects == null || into == null) return;

            foreach (var pair in scope.Objects)
                if (pair.Value is PrefabObject)
                    into.Add(pair.Key);
        }

        // Copies exactly RectObject's members. PlacementOffset is NOT dropped - it says WHEN - so keys are
        // rebased by it, and one rebased outside the span is dropped (Docs/PREFAB_PLACEMENT.md).

        /// <summary> The plain object a flattened placement becomes: same identity, same transform,
        /// no template. </summary>
        public static RectObject Flatten(RectObject placement)
        {
            if (placement == null) return null;

            var flattened = new RectObject();
            flattened.Update(placement);

            if (placement is PrefabObject withOffset && withOffset.PlacementOffset != 0)
                RebaseTracks(flattened, withOffset.PlacementOffset);

            return flattened;
        }

        private static void RebaseTracks(RectObject obj, int offset)
        {
            var lastFrame = FrameRules.LastFrameOf(obj.Span.FrameDuration);

            Rebase(obj.Positions, offset, lastFrame);
            Rebase(obj.Rotations, offset, lastFrame);
            Rebase(obj.Scales, offset, lastFrame);
            Rebase(obj.Sizes, offset, lastFrame);
            Rebase(obj.AnchorsMin, offset, lastFrame);
            Rebase(obj.AnchorsMax, offset, lastFrame);
            Rebase(obj.Pivots, offset, lastFrame);
        }

        // Downward, because removing an entry shifts every index after it.
        private static void Rebase<T>(List<T> keys, int offset, int lastFrame) where T : Keyframe
        {
            if (keys == null) return;

            for (var i = keys.Count - 1; i >= 0; i--)
            {
                var frame = keys[i].Frame - offset;
                if (frame < FrameRules.MinFrame || frame > lastFrame) keys.RemoveAt(i);
                else keys[i].Frame = frame;
            }
        }

        /// <summary> Which templates are still referenced by a placement anywhere in the level -
        /// what a sweep must keep. Appends to <paramref name="into"/>. </summary>
        public static void CollectReferencedPrefabs(IObjectScope scope, HashSet<PrefabId> into)
        {
            if (scope?.Objects == null || into == null) return;

            foreach (var obj in scope.Objects.Values)
                if (obj is PrefabObject placement && placement.PrefabId.IsEnabled())
                    into.Add(placement.PrefabId);
        }
    }
}
