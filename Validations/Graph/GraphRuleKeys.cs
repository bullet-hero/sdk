using System;

namespace BH.SDK.Validations.Graph
{
    // The graph half of what BaseRuleAttribute.RuleNameKey does for declarative rules: a stable,
    // translatable identifier per finding, so a consumer can list rule and graph issues side by side
    // without switching on which kind it is holding. It lives as an extension on the enum rather than
    // as a field on GraphIssue because the mapping is per-rule, not per-occurrence.
    //
    // Deliberately throws on an unmapped value instead of falling back to ToString(): a new GraphRule
    // that nobody named would otherwise reach a player's screen as a raw CLR identifier, and the
    // throw surfaces it in the very first test run that exercises it. Same policy as
    // ObjectConverter.GetType's unknown-ObjectType case.

    /// <summary> Localization-key names for every <see cref="GraphRule"/>. </summary>
    public static class GraphRuleKeys
    {
        public static string GetKey(this GraphRule rule)
        {
            return rule switch
            {
                GraphRule.None => "graph_rule_none",
                GraphRule.DuplicateObjectId => "graph_duplicate_object_id",
                GraphRule.MissingParent => "graph_missing_parent",
                GraphRule.ParentCycle => "graph_parent_cycle",
                GraphRule.ParentTooDeep => "graph_parent_too_deep",
                GraphRule.PrefabCycle => "graph_prefab_cycle",
                GraphRule.PrefabTooDeep => "graph_prefab_too_deep",
                GraphRule.PrefabRemapBroken => "graph_prefab_remap_broken",
                GraphRule.ModificationTargetMissing => "graph_modification_target_missing",
                GraphRule.IdCounterBehind => "graph_id_counter_behind",
                GraphRule.UnresolvedReference => "graph_unresolved_reference",
                GraphRule.BeatSegmentsOverlap => "graph_beat_segments_overlap",
                _ => throw new ArgumentOutOfRangeException(nameof(rule), rule, "Unnamed graph rule"),
            };
        }
    }
}
