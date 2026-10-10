using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;

namespace BH.SDK.Utils
{
    // Placement copies are REBUILDABLE from pfid, ids, mod and off, so Thin keeps them out of the file and
    // Expand rebuilds them on load; the editor still materializes. Expand writes by INDEXER, so a pre-
    // virtualization file (copies AND table) still opens (Docs/PREFAB_PLACEMENT.md).

    /// <summary> Moving a prefab placement's copies out of the file and back in: Thin drops them
    /// before a write, Expand rebuilds them after a read. </summary>
    public static class PrefabVirtualizationUtils
    {
        /// <summary> A level with every materialized copy dropped, ready to be written. Shares every
        /// model instance with the level it was given except the four it has to replace, so the
        /// level handed in is NOT modified and may be the live one. </summary>
        public static Level Thin(Level level)
        {
            if (level?.Game?.Objects == null) return level;

            var thin = level.ShallowClone();

            thin.Game = level.Game.ShallowClone();
            thin.Game.Objects = WithoutMaterializedCopies(level.Game.Objects);

            if (level.Resources?.Prefabs == null) return thin;

            thin.Resources = level.Resources.ShallowClone();
            thin.Resources.Prefabs = new Dictionary<PrefabId, Prefab>(level.Resources.Prefabs.Count);

            foreach (var pair in level.Resources.Prefabs)
            {
                var template = pair.Value;
                if (template?.Objects == null)
                {
                    thin.Resources.Prefabs[pair.Key] = template;
                    continue;
                }

                var thinTemplate = template.ShallowClone();
                thinTemplate.Objects = WithoutMaterializedCopies(template.Objects);
                thin.Resources.Prefabs[pair.Key] = thinTemplate;
            }

            return thin;
        }

        // A NEW dictionary, not removals: slot order is what both writers iterate, and it keeps this
        // non-destructive.

        /// <summary> Every object of a scope except the ones some placement in that same scope
        /// materialized. </summary>
        private static Dictionary<ObjectId, RectObject> WithoutMaterializedCopies(
            Dictionary<ObjectId, RectObject> objects)
        {
            var copies = new HashSet<ObjectId>();
            CollectMaterializedChildIds(objects, copies);

            if (copies.Count == 0) return objects;

            // Capacity is objects.Count, not the difference: a fresh import has ObjectIds and no copies, so
            // the difference goes negative and the constructor throws.
            var kept = new Dictionary<ObjectId, RectObject>(objects.Count);
            foreach (var pair in objects)
                if (!copies.Contains(pair.Key))
                    kept.Add(pair.Key, pair.Value);

            return kept;
        }

        // Shared with PrefabMaterializer.FindPlacements: a copy is an object whose id is a VALUE in a
        // placement's ObjectIds in the SAME scope. A nested placement can be both.

        /// <summary> Ids of every object in this scope that some placement in it materialized. </summary>
        public static void CollectMaterializedChildIds(Dictionary<ObjectId, RectObject> objects,
            HashSet<ObjectId> into)
        {
            if (objects == null || into == null) return;

            foreach (var obj in objects.Values)
            {
                if (obj is not PrefabObject placement || placement.ObjectIds == null) continue;
                foreach (var outerId in placement.ObjectIds.Values)
                    into.Add(outerId);
            }
        }

        /// <summary> Rebuilds every placement's copies, in place, into the level that was read.
        /// The report names everything it could not rebuild. </summary>
        public static PrefabExpandReport Expand(Level level)
        {
            var report = new PrefabExpandReport();
            if (level?.Game?.Objects == null || level.Resources?.Prefabs == null) return report;

            // TEMPLATES FIRST, DEEPEST DEPENDENCY FIRST, and this ordering is correctness rather
            // than tidiness: a template holding a placement of its own flattens that placement's
            // children INTO itself, so whatever copies the template has to find it already whole.
            var expanded = new HashSet<PrefabId>();
            var walking = new HashSet<PrefabId>();
            foreach (var prefabId in level.Resources.Prefabs.Keys)
                ExpandTemplate(prefabId, level, expanded, walking, report, 0);

            ExpandScope(level.Game, LevelScopeName, level, report);
            return report;
        }

        private const string LevelScopeName = "Level";

        private static void ExpandTemplate(PrefabId prefabId, Level level, HashSet<PrefabId> expanded,
            HashSet<PrefabId> walking, PrefabExpandReport report, int depth)
        {
            if (!expanded.Add(prefabId)) return;

            // Both guards are backstops against a file rather than against this code: the graph is
            // validated acyclic when an edge is added (PrefabMaterializer.WouldCreateCycle), and
            // LevelGraphAnalyzer reports PrefabCycle/PrefabTooDeep on what reaches it anyway.
            if (depth >= PrefabRules.MaxInheritanceLevel) return;
            if (!walking.Add(prefabId)) return;

            if (level.Resources.Prefabs.TryGetValue(prefabId, out var template) && template?.Objects != null)
            {
                foreach (var placement in CollectGenuinePlacements(template))
                    ExpandTemplate(placement.PrefabId, level, expanded, walking, report, depth + 1);

                ExpandScope(template, TemplateScopeName(prefabId), level, report);
            }

            walking.Remove(prefabId);
        }

        private static void ExpandScope(IObjectScope scope, string scopeName, Level level,
            PrefabExpandReport report)
        {
            // The list is taken BEFORE anything is written, because expanding one placement adds
            // objects to the very dictionary the genuine/copy test reads.
            foreach (var placement in CollectGenuinePlacements(scope))
                ExpandPlacement(placement, scope, scopeName, level, report);
        }

        /// <summary> Placements a scope owns in its own right, i.e. not themselves copies some other
        /// placement in that scope materialized. </summary>
        private static List<PrefabObject> CollectGenuinePlacements(IObjectScope scope)
        {
            var result = new List<PrefabObject>();
            if (scope?.Objects == null) return result;

            var copies = new HashSet<ObjectId>();
            CollectMaterializedChildIds(scope.Objects, copies);

            foreach (var obj in scope.Objects.Values)
                if (obj is PrefabObject placement && !copies.Contains(placement.ObjectId))
                    result.Add(placement);

            return result;
        }

        private static void ExpandPlacement(PrefabObject placement, IObjectScope hostScope,
            string scopeName, Level level, PrefabExpandReport report)
        {
            // An empty placement is a real authored state, not a dangling reference - every
            // placement is created this way before the author picks a template.
            if (!placement.PrefabId.IsEnabled()) return;

            report.CountPlacement();

            if (!level.Resources.Prefabs.TryGetValue(placement.PrefabId, out var template)
                || template?.Objects == null)
            {
                report.Report(PrefabExpandProblem.TemplateMissing, scopeName, placement.ObjectId,
                    $"prefab {placement.PrefabId.value} is not in this level's resources");
                return;
            }

            if (ObjectDepthUtils.WouldExceedDepth(hostScope, placement, template))
            {
                report.Report(PrefabExpandProblem.TooDeep, scopeName, placement.ObjectId,
                    $"prefab {placement.PrefabId.value} would nest deeper than {LevelRules.MaxObjectDepth}");
                return;
            }

            // The root is written before and independently of the id table: the placement IS its copy.
            PrefabRootUtils.ApplyRoot(placement, template);

            if (template.Objects.Count == 0)
            {
                ApplyModifications(placement, hostScope);
                return;
            }

            // One finding for a whole missing table, one per entry for a partial one.
            if (placement.ObjectIds == null || placement.ObjectIds.Count == 0)
            {
                report.Report(PrefabExpandProblem.RemapMissing, scopeName, placement.ObjectId,
                    $"placement owns no id table, so none of prefab {placement.PrefabId.value} is rebuilt");
                ApplyModifications(placement, hostScope);
                return;
            }

            var written = 0;
            foreach (var innerObj in template.Objects.Values)
            {
                // NEVER A SILENT MINT. An outer id is born at edit time and is addressable from
                // outside the placement (an ordinary object may be reparented under a copy), so
                // inventing one here would hand the level an object nothing else in the file names.
                if (!placement.ObjectIds.TryGetValue(innerObj.ObjectId, out var outerId))
                {
                    report.Report(PrefabExpandProblem.RemapMissing, scopeName, placement.ObjectId,
                        $"template object {innerObj.ObjectId.value} has no id of its own here");
                    continue;
                }

                var outerObj = innerObj.Copy();
                outerObj.ObjectId = outerId;
                ApplyPlacementFrameOffset(outerObj, placement);
                hostScope.Objects[outerId] = outerObj;
                written++;
            }

            RemapParents(placement, template, hostScope);
            ApplyModifications(placement, hostScope);
            report.CountExpanded(written);
        }

        // Template spans are local to the prefab timeline. Compose with ToGlobalFrame, never a bare
        // addition: a local frame is a FRAME, and adding counts the origin twice. Lockstep:
        // PrefabMaterializer.

        /// <summary> Resolves a copy's template-local span into the host scope's own timeline,
        /// pulled back by the placement's own in-point. </summary>
        private static void ApplyPlacementFrameOffset(RectObject outerObj, PrefabObject placement)
        {
            outerObj.Span = outerObj.Span.WithStart(
                placement.Span.ToGlobalFrame(outerObj.Span.StartFrame) - placement.PlacementOffset);
        }

        // Null and ObjectId.PrefabRoot both auto-parent to the placement; a dangling inner reference is left
        // for the rule system to flag.

        /// <summary> Points every rebuilt copy's parent at the copy (or the placement) it belongs to. </summary>
        private static void RemapParents(PrefabObject placement, Prefab template, IObjectScope hostScope)
        {
            foreach (var pair in placement.ObjectIds)
            {
                if (!template.Objects.TryGetValue(pair.Key, out var innerObj)) continue;
                if (!hostScope.Objects.TryGetValue(pair.Value, out var outerObj)) continue;

                var innerParentId = innerObj.ParentObjectId;
                outerObj.ParentObjectId = !innerParentId.IsValid() || innerParentId == ObjectId.PrefabRoot
                    ? placement.ObjectId
                    : placement.ObjectIds.GetValueOrDefault(innerParentId, innerParentId);
            }
        }

        // ALWAYS LAST, after every other step, exactly as at edit time: the pipeline is "discard the
        // copy, re-copy from template, re-apply overrides", never an in-place patch. Any path that
        // rebuilds a placement's subtree and does not end here loses every override on it.

        /// <summary> Re-applies this placement's per-instance overrides onto the fresh copies. </summary>
        private static void ApplyModifications(PrefabObject placement, IObjectScope hostScope)
        {
            if (placement.Modifications == null) return;

            foreach (var pair in placement.Modifications)
            {
                if (!PrefabRootUtils.TryGetModificationTarget(placement, hostScope, pair.Key.ObjectId,
                        out var outerObj)) continue;
                outerObj.Apply(pair.Value);
            }
        }

        private static string TemplateScopeName(PrefabId prefabId) => $"Prefab[{prefabId.value}]";
    }
}