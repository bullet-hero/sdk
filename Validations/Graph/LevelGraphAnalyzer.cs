using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Models.Events;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Rules;

namespace BH.SDK.Validations.Graph
{
    // The half of the standard the declarative rules structurally cannot cover. A [RuleXxx] sees one
    // value, an object rule sees one instance; everything about how objects relate - who owns which
    // id, what a parent points at, whether a template places itself - needs the whole level at once.
    //
    // Written as an explicit walk rather than reflection, unlike RuleAnalyzer: there are a handful of
    // invariants, each with its own shape, and a generic engine for them would be more machinery than
    // the invariants themselves.

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

        // "Where does the grid run, and how fast" must have exactly one answer per frame, and a
        // property attribute cannot see a second segment to compare against - which is what puts this
        // here rather than on BeatSegment.Span. Reported per PAIR, not per segment: a segment is only
        // ever wrong relative to another one, and naming both is what makes the finding actionable.
        //
        // Order is not assumed. The list is authored, and nothing in the format sorts it, so a
        // segment can legally sit before its predecessor in the list while its span sits after it.

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

                // A child whose span reaches outside its parent's is deliberately NOT reported. It is
                // legal authored data: a consumer resolves the containment on read rather than
                // storing the clipped value (the Unity project's FrameSpanFitMath), so the overhang
                // never plays and comes back the moment the parent grows again. Saying so on every
                // load turned out to be noise about content that is behaving exactly as designed -
                // fitting the lifetimes is now an edit the author asks for (mod_span_fit), not a
                // finding that follows them around.

                current = parent;
            }

            result.Add(new GraphIssue(GraphRule.ParentTooDeep, RuleGroup.Error,
                $"{scopeName}.Objects[{obj.ObjectId}]",
                $"parent chain is deeper than {LevelRules.MaxObjectDepth}"));
        }

        // A counter at or below an id already in use hands the next created object a colliding id -
        // the one failure here that is silent at authoring time and corrupt afterwards.
        private static void AnalyzeIdCounter(Dictionary<ObjectId, RectObject> objects, int counter,
            string scopeName, List<GraphIssue> result)
        {
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

        #endregion

        #region Placements: remaps and overrides

        private static void AnalyzePlacements(Level level, IObjectScope scope,
            string scopeName, List<GraphIssue> result)
        {
            foreach (var pair in scope.Objects)
            {
                if (pair.Value is not PrefabObject placement) continue;

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
