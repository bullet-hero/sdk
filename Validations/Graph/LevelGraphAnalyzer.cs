using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Events;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;

namespace BH.SDK.Validations.Graph
{
    // The half the declarative rules structurally cannot cover: how objects relate (ids, parents,
    // placements) needs the whole level. An explicit walk rather than reflection - a handful of
    // invariants, each with its own shape.

    /// <summary>
    /// Checks the cross-object invariants of a whole Level: id uniqueness and counters, parent
    /// resolution and cycles, prefab nesting and cycles, placement remaps and overrides.
    /// </summary>
    public class LevelGraphAnalyzer
    {
        /// <summary> Every relational finding a whole level has - the ones a single property could never see. </summary>
        public List<GraphIssue> Analyze(Level level)
        {
            var result = new List<GraphIssue>();
            if (level?.Game?.Objects == null || level.Resources == null) return result;

            AnalyzeScope(level.Game, "Level", result);
            AnalyzeIdCounter(level.Game.Objects, level.Settings?.ObjectIdCounter ?? 0, "Level", result);
            AnalyzePlacements(level, level.Game, "Level", result);
            AnalyzeBeatSegments(level.Game.Events?.Beats, result);

            foreach (var pair in level.Resources.Prefabs)
            {
                var prefab = pair.Value;
                if (prefab?.Objects == null) continue;

                var scopeName = $"Prefab[{pair.Key}]";
                AnalyzeScope(prefab, scopeName, result);
                AnalyzeIdCounter(prefab.Objects, prefab.ObjectIdCounter, scopeName, result);
                AnalyzePlacements(level, prefab, scopeName, result);
            }

            AnalyzePrefabNesting(level, result);
            return result;
        }

        #region Beat segments

        // Here, not on BeatSegment.Span: a property attribute cannot see a second segment. Reported per
        // PAIR, and order is not assumed - the list is authored and a segment may precede its predecessor.

        private static void AnalyzeBeatSegments(List<BeatSegment> beats, List<GraphIssue> result)
        {
            if (beats == null) return;

            for (var i = 0; i < beats.Count; i++)
            {
                var left = beats[i];
                if (left == null) continue;

                for (var j = i + 1; j < beats.Count; j++)
                {
                    var right = beats[j];
                    if (right == null || !left.Span.Overlaps(right.Span)) continue;

                    result.Add(new GraphIssue(GraphRule.BeatSegmentsOverlap, RuleGroup.Error,
                        $"Level.Game.Events.Beats[{i}]",
                        $"beat segment {left.Span} overlaps Beats[{j}] {right.Span}"));
                }
            }
        }

        #endregion

        #region Scope: ids and parents

        private static void AnalyzeScope(IObjectScope scope, string scopeName, List<GraphIssue> result)
        {
            var seenIds = new HashSet<ObjectId>();

            foreach (var pair in scope.Objects)
            {
                var obj = pair.Value;
                if (obj == null) continue;

                // The dictionary key is unique by construction; the id INSIDE the value is not, and
                // that is the one every reference resolves against.
                if (!seenIds.Add(obj.ObjectId))
                {
                    result.Add(new GraphIssue(GraphRule.DuplicateObjectId, RuleGroup.Error,
                        $"{scopeName}.Objects[{pair.Key}]",
                        $"ObjectId {obj.ObjectId} is claimed by more than one object"));
                }
            }

            foreach (var pair in scope.Objects)
            {
                var obj = pair.Value;
                if (obj == null) continue;

                AnalyzeParentChain(scope, obj, scopeName, result);
            }
        }

        private static void AnalyzeParentChain(IObjectScope scope, RectObject obj,
            string scopeName, List<GraphIssue> result)
        {
            var current = obj;
            var visited = new HashSet<ObjectId> { obj.ObjectId };

            for (var depth = 0; depth <= LevelRules.MaxObjectDepth; depth++)
            {
                var parentId = current.ParentObjectId;

                // Null and the reserved negatives terminate a chain - they are not objects in this
                // scope and RuleParentObjectIdValid already judged whether they belong here.
                if (!parentId.IsValid()) return;

                if (!scope.Objects.TryGetValue(parentId, out var parent) || parent == null)
                {
                    result.Add(new GraphIssue(GraphRule.MissingParent, RuleGroup.Error,
                        $"{scopeName}.Objects[{obj.ObjectId}]",
                        $"ParentObjectId {parentId} resolves to nothing in this scope"));
                    return;
                }

                if (!visited.Add(parentId))
                {
                    result.Add(new GraphIssue(GraphRule.ParentCycle, RuleGroup.Error,
                        $"{scopeName}.Objects[{obj.ObjectId}]",
                        $"parent chain loops back through {parentId}"));
                    return;
                }

                // An overhanging child is NOT reported: legal data, resolved on read (FrameSpanFitMath), and
                // fitting lifetimes is an edit the author asks for (mod_span_fit).

                current = parent;
            }

            result.Add(new GraphIssue(GraphRule.ParentTooDeep, RuleGroup.Error,
                $"{scopeName}.Objects[{obj.ObjectId}]",
                $"parent chain is deeper than {LevelRules.MaxObjectDepth}"));
        }

        // A counter at or below a used id hands the next object a colliding id: silent, then corrupt. The
        // second finding is a counter running out (ids are never reused); checked WITHOUT the empty early
        // return above, since an emptied level near the ceiling is the case it is for.
        private static void AnalyzeIdCounter(Dictionary<ObjectId, RectObject> objects, int counter,
            string scopeName, List<GraphIssue> result)
        {
            if (LevelRules.IsObjectIdCounterNearExhaustion(counter))
            {
                result.Add(new GraphIssue(GraphRule.IdCounterNearExhaustion, RuleGroup.Warning,
                    $"{scopeName}.ObjectIdCounter",
                    $"counter {counter} has spent {ObjectIdPercentSpent(counter)}% of the id space - " +
                    $"{LevelRules.RemainingObjectIds(counter)} ids left, and they are never reused"));
            }

            var maxUsed = int.MinValue;
            foreach (var pair in objects)
            {
                if (pair.Key.value > maxUsed) maxUsed = pair.Key.value;
            }

            if (maxUsed == int.MinValue) return;

            if (counter <= maxUsed)
            {
                result.Add(new GraphIssue(GraphRule.IdCounterBehind, RuleGroup.Error,
                    $"{scopeName}.ObjectIdCounter",
                    $"counter {counter} is not past the highest id in use ({maxUsed})"));
            }
        }

        private static int ObjectIdPercentSpent(int counter) =>
            (int)(100L - LevelRules.RemainingObjectIds(counter) * 100L / LevelRules.MaxObjectIds);

        #endregion

        #region Placements: remaps and overrides

        // One case narrower than PrefabVirtualizationUtils.CollectMaterializedChildIds, deliberately: a
        // placement whose own ObjectIds names ITSELF is broken data, and hiding it would silence its rule.
        private static void CollectCopiesOfOtherPlacements(Dictionary<ObjectId, RectObject> objects,
            HashSet<ObjectId> into)
        {
            foreach (var obj in objects.Values)
            {
                if (obj is not PrefabObject placement || placement.ObjectIds == null) continue;
                foreach (var outerId in placement.ObjectIds.Values)
                    if (outerId != placement.ObjectId)
                        into.Add(outerId);
            }
        }

        // ONLY A GENUINE PLACEMENT IS ASKED THESE QUESTIONS: a PrefabObject that is a materialized COPY
        // names ids in the INNER template's scope. Reading it as a placement reports every remap as
        // broken (PrefabMaterializer.FindPlacements has the long form).
        private static void AnalyzePlacements(Level level, IObjectScope scope,
            string scopeName, List<GraphIssue> result)
        {
            var materializedChildIds = new HashSet<ObjectId>();
            CollectCopiesOfOtherPlacements(scope.Objects, materializedChildIds);

            foreach (var pair in scope.Objects)
            {
                if (pair.Value is not PrefabObject placement) continue;
                if (materializedChildIds.Contains(pair.Key)) continue;

                var path = $"{scopeName}.Objects[{pair.Key}]";

                // An empty placement is a real authored state, not a dangling reference.
                if (!placement.PrefabId.IsEnabled()) continue;

                if (!level.Resources.Prefabs.TryGetValue(placement.PrefabId, out var template)
                    || template?.Objects == null)
                {
                    result.Add(new GraphIssue(GraphRule.UnresolvedReference, RuleGroup.Error, path,
                        $"PrefabId {placement.PrefabId} names no template in this level"));
                    continue;
                }

                foreach (var remap in placement.ObjectIds)
                {
                    if (!template.Objects.ContainsKey(remap.Key))
                    {
                        result.Add(new GraphIssue(GraphRule.PrefabRemapBroken, RuleGroup.Warning, path,
                            $"remap source {remap.Key} is not an object of the template"));
                    }

                    if (!scope.Objects.ContainsKey(remap.Value))
                    {
                        result.Add(new GraphIssue(GraphRule.PrefabRemapBroken, RuleGroup.Warning, path,
                            $"remap target {remap.Value} is not materialized in this scope"));
                    }
                }

                foreach (var modification in placement.Modifications)
                {
                    if (template.Objects.ContainsKey(modification.Key.ObjectId)) continue;

                    // THE ROOT IS A FIELD (Prefab.Root), NOT IN Objects: a dictionary probe reads every
                    // root-addressed override as dangling. Report only a field the root does not own, which
                    // ApplyModifications would drop (PrefabRootUtils.TryGetModificationTarget).
                    if (modification.Key.ObjectId == ObjectId.PrefabRoot)
                    {
                        if (ModificationFields.IsPrefabRootField(modification.Key.Field)) continue;

                        result.Add(new GraphIssue(GraphRule.ModificationTargetMissing, RuleGroup.Warning, path,
                            $"override targets the template root with field {modification.Key.Field}, " +
                            "which the root does not own"));
                        continue;
                    }

                    result.Add(new GraphIssue(GraphRule.ModificationTargetMissing, RuleGroup.Warning, path,
                        $"override targets {modification.Key.ObjectId}, which the template no longer has"));
                }
            }
        }

        #endregion

        #region Prefab nesting

        // Depth and cycles are one walk: a cycle is what an unbounded descent turns into, and both
        // make materialization non-terminating rather than merely wrong.
        private static void AnalyzePrefabNesting(Level level, List<GraphIssue> result)
        {
            foreach (var pair in level.Resources.Prefabs)
            {
                if (pair.Value?.Objects == null) continue;

                var chain = new HashSet<PrefabId> { pair.Key };
                Descend(level, pair.Value, pair.Key, chain, 0, result);
            }
        }

        private static void Descend(Level level, Prefab template, PrefabId rootId,
            HashSet<PrefabId> chain, int depth, List<GraphIssue> result)
        {
            if (depth > PrefabRules.MaxInheritanceLevel)
            {
                result.Add(new GraphIssue(GraphRule.PrefabTooDeep, RuleGroup.Error,
                    $"Prefab[{rootId}]",
                    $"nesting is deeper than {PrefabRules.MaxInheritanceLevel}"));
                return;
            }

            foreach (var pair in template.Objects)
            {
                if (pair.Value is not PrefabObject placement) continue;
                if (!placement.PrefabId.IsEnabled()) continue;

                if (!chain.Add(placement.PrefabId))
                {
                    result.Add(new GraphIssue(GraphRule.PrefabCycle, RuleGroup.Error,
                        $"Prefab[{rootId}].Objects[{pair.Key}]",
                        $"template {placement.PrefabId} transitively places itself"));
                    continue;
                }

                if (level.Resources.Prefabs.TryGetValue(placement.PrefabId, out var nested)
                    && nested?.Objects != null)
                {
                    Descend(level, nested, rootId, chain, depth + 1, result);
                }

                chain.Remove(placement.PrefabId);
            }
        }

        #endregion
    }
}