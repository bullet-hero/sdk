using System;

namespace BH.SDK.Validations.Graph
{
    // The graph twin of BaseRuleAttribute.RuleNameKey: a stable translatable key per finding. Throws on an
    // unmapped value rather than ToString(), so an unnamed rule is caught by the first test that hits it.

    /// <summary> Localization-key names for every <see cref="GraphRule"/>. </summary>
    public static class GraphRuleKeys
    {
        /// <summary> The localization key naming a finding, so it survives a rename and can be translated. </summary>
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
                GraphRule.IdCounterNearExhaustion => "graph_id_counter_near_exhaustion",
                _ => throw new ArgumentOutOfRangeException(nameof(rule), rule, "Unnamed graph rule"),
            };
        }
    }
}
