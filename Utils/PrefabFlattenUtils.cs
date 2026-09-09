using System.Collections.Generic;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;

namespace BH.SDK.Utils
{
    // Flattening is the inverse of placing: it drops the bookkeeping that ties a PrefabObject to its
    // template and leaves the copies it already owns behind as ordinary objects. Nothing is created,
    // moved or re-materialized - the copies are permanent, positively-id'd objects the moment a
    // PrefabId is assigned (the Unity project's PrefabMaterializer), and a placement's Modifications
    // are applied ONTO those copies rather than resolved on read, so dropping the table loses no
    // authored value.
    //
    // FLATTENING ONE PLACEMENT MUST FLATTEN EVERY PrefabObject IT OWNS, AND THAT IS CORRECTNESS
    // RATHER THAN CONVENIENCE. A template holding a nested placement materializes FLAT: the nested
    // placement is copied as an ordinary entry that still happens to be PrefabObject-typed, and its
    // own ObjectIds table names ids in the TEMPLATE's scope, not the host's. PrefabMaterializer
    // .FindPlacements only knows to skip such a copy because its id appears as a VALUE in the outer
    // placement's ObjectIds. Clear that table and the copy starts reading as a genuine placement, so
    // the next resync of the inner template writes refreshed objects over host ids that belong to
    // something else entirely. Both entry points here therefore return the whole chain, never a
    // single object.

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

        // The genuine/materialized-copy distinction PrefabMaterializer.FindPlacements draws is
        // deliberately NOT drawn here, and the reason is that this sweep takes every one of them:
        // whichever way a PrefabObject got into the scope, it has to stop being one, and walking the
        // chains would only reach the same set the slower way.

        /// <summary> Every PrefabObject in a scope - what a whole-scope flatten has to touch.
        /// Appends to <paramref name="into"/>. </summary>
        public static void CollectAll(IObjectScope scope, List<ObjectId> into)
        {
            if (scope?.Objects == null || into == null) return;

            foreach (var pair in scope.Objects)
                if (pair.Value is PrefabObject)
                    into.Add(pair.Key);
        }

        // Update copies exactly the members RectObject declares - id, parent, name, active, span,
        // layer and all seven keyframe tracks - so the placement keeps its place in the hierarchy and
        // its children keep pointing at it. PrefabId, ObjectIds and Modifications simply have nowhere
        // to go, which is the whole operation.

        /// <summary> The plain object a flattened placement becomes: same identity, same transform,
        /// no template. </summary>
        public static RectObject Flatten(RectObject placement)
        {
            if (placement == null) return null;

            var flattened = new RectObject();
            flattened.Update(placement);
            return flattened;
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
