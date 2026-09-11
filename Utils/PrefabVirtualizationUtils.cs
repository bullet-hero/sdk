using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;

namespace BH.SDK.Utils
{
    // A placement's materialized copies are REBUILDABLE, and this is the pair of functions that
    // says so: pfid names the template, ids maps every template-inner id to the copy's permanent
    // outer id, and mod carries the per-instance overrides. Nothing else is needed, so the copies
    // stop reaching the file and are rebuilt on the way back in - measured at 62-82% of four corpus
    // levels' bytes, 13% of volcano's, and gzip does not absorb it (each copy carries its own id and
    // its own span offset, and deflate's window is 32 KB). Docs/Issues/PREFAB_VIRTUALIZATION_HISTORY.md
    // is the record.
    //
    // THE EDITOR STILL MATERIALIZES. Thin and Expand bracket the FILE, not the model: by the time a
    // level plays, the same flat GameLevel.Objects map exists as before, and the editor, every
    // operation, LevelStateBuilder and every job keep seeing ordinary objects. The consuming Unity
    // project's BH.Core.Services.PrefabMaterializer is unchanged and remains the only thing that
    // MINTS an outer id - ids are born at edit time, never at load time, which is exactly what makes
    // the table in the file authoritative.
    //
    // EXPANSION WRITES BY INDEXER RATHER THAN BY Add, and that one character is what lets a level
    // saved BEFORE virtualization still open: such a file holds its flat copies AND its id table, so
    // an Add would throw or double them while an indexer overwrites them in place. No migration, no
    // drop pass, nothing Rule 11 would call a shim.

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

        // A NEW DICTIONARY RATHER THAN REMOVALS FROM THE OLD ONE. Removing and re-adding reshuffles
        // a Dictionary's entry slots, and slot order is what both writers iterate - so the same
        // level would write different bytes depending on whether it had been thinned before. It also
        // keeps this non-destructive, which is what lets a caller hand over the live level.

        /// <summary> Every object of a scope except the ones some placement in that same scope
        /// materialized. </summary>
        private static Dictionary<ObjectId, RectObject> WithoutMaterializedCopies(
            Dictionary<ObjectId, RectObject> objects)
        {
            var copies = new HashSet<ObjectId>();
            CollectMaterializedChildIds(objects, copies);

            if (copies.Count == 0) return objects;

            var kept = new Dictionary<ObjectId, RectObject>(objects.Count - copies.Count);
            foreach (var pair in objects)
                if (!copies.Contains(pair.Key))
                    kept.Add(pair.Key, pair.Value);

            return kept;
        }

        // The one predicate this whole feature turns on, and it is deliberately shared with the
        // edit-time materializer (BH.Core.Services.PrefabMaterializer.FindPlacements) rather than
        // restated: an object is a materialized copy exactly when its id appears as a VALUE in some
        // placement's ObjectIds within the SAME scope. A PrefabObject can be both - a template
        // holding a nested placement materializes flat, so the nested placement arrives in the host
        // as an ordinary entry that still happens to be PrefabObject-typed, and its own table names
        // ids in the TEMPLATE's scope rather than this one.

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

            if (template.Objects.Count == 0) return;

            // ONE FINDING FOR A WHOLE MISSING TABLE, one per entry for a partial one. A placement
            // that was never materialized - an Afterbeat import before its host resynced, or one
            // the materializer refused on depth - owns no table at all, and reporting that per
            // template object would file a thousand findings for a single authoring state.
            if (placement.ObjectIds == null || placement.ObjectIds.Count == 0)
            {
                report.Report(PrefabExpandProblem.RemapMissing, scopeName, placement.ObjectId,
                    $"placement owns no id table, so none of prefab {placement.PrefabId.value} is rebuilt");
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
                ApplyPlacementFrameOffset(outerObj, placement.Span);
                hostScope.Objects[outerId] = outerObj;
                written++;
            }

            RemapParents(placement, template, hostScope);
            ApplyModifications(placement, hostScope);
            report.CountExpanded(written);
        }

        // A template's own objects store their span LOCAL to the prefab's own timeline - the exact
        // same "local" relationship a keyframe's Frame has to its owning object's span start, and
        // for the same reason: it is what makes one template placeable at multiple different start
        // frames and still mean the same thing relative to each. Only the start moves: a duration is
        // the same number in any coordinate system.
        //
        // ToGlobalFrame RATHER THAN A BARE ADDITION, because a local frame is a FRAME and not an
        // offset: the placement's own first frame is local FrameRules.MinFrame, so composing two
        // positions has to give the origin back exactly once. Adding them outright counts it twice
        // and puts every copy one frame late - which is precisely what the edit-time materializer
        // had been doing since it moved onto FrameSpan, invisibly, because the copies used to be
        // stored rather than recomputed. The two halves must agree, and this is the formula they
        // agree on; BH.Core.Services.PrefabMaterializer carries the same note.

        /// <summary> Resolves a copy's template-local span into the host scope's own timeline. </summary>
        private static void ApplyPlacementFrameOffset(RectObject outerObj, in FrameSpan placementSpan)
        {
            outerObj.Span = outerObj.Span.WithStart(placementSpan.ToGlobalFrame(outerObj.Span.StartFrame));
        }

        // An inner object whose ParentObjectId is unset (Null) OR explicitly ObjectId.PrefabRoot
        // both auto-parent to the placement itself - the two are equivalent by design. An inner
        // object naming another inner object keeps pointing at it, remapped through this
        // placement's own table; a dangling inner reference is left unremapped, exactly like a
        // hand-authored object's dangling ParentObjectId - an authoring error for the rule system
        // to flag, not something silently fixed up here.

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
                if (!placement.ObjectIds.TryGetValue(pair.Key.ObjectId, out var outerId)) continue;
                if (!hostScope.Objects.TryGetValue(outerId, out var outerObj)) continue;
                outerObj.Apply(pair.Value);
            }
        }

        private static string TemplateScopeName(PrefabId prefabId) => $"Prefab[{prefabId.value}]";
    }
}