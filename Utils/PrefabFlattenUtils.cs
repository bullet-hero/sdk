using System.Collections.Generic;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Keyframes;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;

namespace BH.SDK.Utils
{
    // Flattening is the inverse of placing: it drops the bookkeeping that ties a PrefabObject to its
    // template and leaves the copies it already owns behind as ordinary objects. Nothing is created,
    // moved or re-materialized - the copies are permanent, positively-id'd objects the moment a
    // PrefabId is assigned (the Unity project's PrefabMaterializer), and a placement's Modifications
    // are applied ONTO those copies rather than resolved on read, so dropping the table loses no
    // authored value.
    //
    // A FLATTEN MUST ANSWER FOR EVERY PrefabObject THE PLACEMENT OWNS, AND THAT IS CORRECTNESS
    // RATHER THAN CONVENIENCE. A template holding a nested placement materializes FLAT: the nested
    // placement is copied as an ordinary entry that still happens to be PrefabObject-typed, and its
    // own ObjectIds table names ids in the TEMPLATE's scope, not the host's. PrefabMaterializer
    // .FindPlacements only knows to skip such a copy because its id appears as a VALUE in the outer
    // placement's ObjectIds. Clear that table and say nothing else, and the copy starts reading as a
    // genuine placement whose table points at ids in a scope the host knows nothing about - so the
    // next resync of the inner template writes refreshed objects over host ids belonging to
    // something else entirely.
    //
    // THERE ARE EXACTLY TWO ANSWERS TO THAT, AND BOTH ARE HERE. Flattening the whole CHAIN is one:
    // nothing is left holding a table. Flattening the TOP ALONE is the other, and it is not the
    // absence of the first - the nested copy stays a placement, still linked to its own template,
    // and its table is REBASED into the host's ids first (TryRebaseNested). Those ids are exactly
    // what the outer table maps: it reads outer-template id -> host id, and a nested copy's values
    // ARE outer-template ids, so one lookup per entry moves the copy into the scope it now lives in.
    // The rebase happens while the outer table is still intact, which is what makes it possible at
    // all - after the flatten there is nothing left to look the ids up in.
    //
    // A top-only flatten that cannot rebase some copy is not performed on that copy: the caller is
    // told, and flattens it too. A half-rebased table is the one state neither answer survives.

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
        //
        // PlacementOffset HAS NOWHERE TO GO EITHER AND IS NOT DROPPED: it is the one of the four
        // that says WHEN, so dropping it would move the picture. A placement reads its tracks from
        // Span.StartFrame - PlacementOffset (PlacementTrimMath.InstanceSpan) and a plain RectObject
        // reads its own from Span.StartFrame, so the keys are rebased by that difference here -
        // baked, exactly as every materialized copy's span already was at materialize time.
        // Its CHILDREN need nothing: their spans carry the subtraction already.
        //
        // A key rebased outside the span is DROPPED, which is the one lossy thing a flatten does to
        // an offset placement, and it is the same call the cut makes (ObjectSplitMath.MapKeyframe).
        // A placement with no offset - every placement until one is trimmed or cut - is untouched.

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
