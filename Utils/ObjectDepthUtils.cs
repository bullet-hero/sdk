using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;

namespace BH.SDK.Utils
{
    // Layer is summed up the parent chain and the chain itself is bounded: LevelRules.MaxObjectDepth
    // is what authored content may reach, one below the Unity player's own MaxChildInherit, which is
    // the size of BuildInstancesParentingJob's stackalloc. Past that cap the runtime composes an
    // object against a mid-chain ancestor instead of its root: no exception, no validation failure,
    // just an object drawn in the wrong place. So every edit that can deepen a chain - reparenting,
    // materializing a placement, expanding one at load - asks here first.
    //
    // THIS LIVES IN THE SDK RATHER THAN BESIDE THE EDIT-TIME MATERIALIZER, and that is what keeps
    // one answer instead of two: the load-time expander (PrefabVirtualizationUtils) is SDK code and
    // cannot reach BH.Core, while the editor's own refusals are in BH.Core and cannot be moved. The
    // consuming project's BH.Core.Utils.HierarchyMath is now a forwarding facade over this class,
    // kept only for the IL2CPP options it carries and for its existing callers.
    //
    // Every walk is bounded by the cap itself: this reads author data, which can be cyclic (the
    // per-property rules don't catch cycles - LevelGraphAnalyzer does, separately).

    /// <summary> Parent-chain depth math over an IObjectScope, shared by every edit that can make a
    /// chain deeper and by the load-time prefab expansion. </summary>
    public static class ObjectDepthUtils
    {
        // Node count, not ancestor count, because that is what the callers need: an object parented
        // under X has exactly GetDepth(X) ancestors (X itself plus everything above it). ObjectId
        // .Null - the scope root - is 0, so a root-level object comes out with zero ancestors.

        /// <summary> How many objects lie on the chain from this one up to the scope root, itself
        /// included: 0 for Null, 1 for a root-level object. Returns -1 when the chain is longer than
        /// LevelRules.MaxObjectDepth, which a cycle also reads as - both are refusals for every
        /// caller here. </summary>
        public static int GetDepth(IObjectScope scope, ObjectId id)
        {
            var depth = 0;
            while (id.IsNotNull())
            {
                if (!scope.Objects.TryGetValue(id, out var obj)) return depth;
                if (++depth > LevelRules.MaxObjectDepth) return -1;
                id = obj.ParentObjectId;
            }
            return depth;
        }

        /// <summary> Longest chain of descendants below an object: 0 when it has no children. Walks
        /// every object's parent chain rather than maintaining a child index, which nothing else in
        /// the project needs - fine for a one-off author action, not for a per-frame path. </summary>
        public static int GetSubtreeHeight(IObjectScope scope, ObjectId id)
        {
            var height = 0;
            foreach (var pair in scope.Objects)
            {
                var distance = GetDistanceToAncestor(scope, pair.Key, id);
                if (distance > height) height = distance;
            }
            return height;
        }

        /// <summary> How many steps up from an object its given ancestor sits, or 0 when that
        /// ancestor is not on its chain at all. </summary>
        public static int GetDistanceToAncestor(IObjectScope scope, ObjectId id, ObjectId ancestorId)
        {
            var steps = 0;
            while (id.IsNotNull() && steps <= LevelRules.MaxObjectDepth)
            {
                if (id == ancestorId) return steps;
                if (!scope.Objects.TryGetValue(id, out var obj)) return 0;
                id = obj.ParentObjectId;
                steps++;
            }
            return 0;
        }

        /// <summary> Whether moving an object (with everything under it) onto a new parent would
        /// push some chain past the cap. The moved subtree's HEIGHT is what usually breaches it, not
        /// the moved object alone. </summary>
        public static bool WouldExceedDepth(IObjectScope scope, ObjectId movedId, ObjectId newParentId)
        {
            var parentDepth = GetDepth(scope, newParentId);
            if (parentDepth < 0) return true; // already broken or cyclic - do not add to it

            // parentDepth IS the moved object's new ancestor count, and the deepest thing under it
            // sits subtreeHeight levels further down.
            return parentDepth + GetSubtreeHeight(scope, movedId) > LevelRules.MaxObjectDepth;
        }

        // A template's inner objects are copied into the host scope UNDER the placement, so the
        // template's own internal depth stacks onto the placement's. Inner parents are template-
        // local: Null and the reserved PrefabRoot both mean "template root" (see
        // PrefabVirtualizationUtils.RemapParents), and anything else names another inner object.

        /// <summary> Deepest chain inside a prefab template, measured from its own root: 0 when
        /// every object in it sits at the template's top level. </summary>
        public static int GetTemplateDepth(Prefab template)
        {
            var deepest = 0;
            foreach (var inner in template.Objects.Values)
            {
                var depth = 0;
                var parentId = inner.ParentObjectId;
                while (parentId.IsNotNull() && parentId != ObjectId.PrefabRoot)
                {
                    if (!template.Objects.TryGetValue(parentId, out var parent)) break;
                    if (++depth > LevelRules.MaxObjectDepth) return -1;
                    parentId = parent.ParentObjectId;
                }
                if (depth > deepest) deepest = depth;
            }
            return deepest;
        }

        /// <summary> Whether materializing a template under a placement would push some chain past
        /// the cap: the placement's own depth, the level the copies sit at under it, and the
        /// template's own internal depth. </summary>
        public static bool WouldExceedDepth(IObjectScope hostScope, PrefabObject placement, Prefab template)
        {
            var placementDepth = GetDepth(hostScope, placement.ObjectId);
            if (placementDepth < 0) return true;

            var templateDepth = GetTemplateDepth(template);
            if (templateDepth < 0) return true;

            // A copy of a template ROOT object is parented to the placement, so it carries
            // placementDepth ancestors; the template's own nesting adds to that.
            return placementDepth + templateDepth > LevelRules.MaxObjectDepth;
        }
    }
}
