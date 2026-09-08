using System.Collections.Generic;
using System.Linq;
using BH.SDK.Rules;
using BH.SDK.Validations.Graph;

namespace BH.SDK.Validations
{
    /// <summary>
    /// Everything one validation pass found, from both halves of the standard: the declarative rules
    /// and the cross-object graph. Kept as two lists rather than one merged sequence, because they
    /// address findings differently - a rule issue points at a property and can often repair itself,
    /// a graph issue points at a relationship and never can.
    /// </summary>
    public readonly struct ValidationReport
    {
        /// <summary> Everything the declarative rules found. </summary>
        public readonly List<RuleIssue> RuleIssues;

        /// <summary> Everything the relational pass found. </summary>
        public readonly List<GraphIssue> GraphIssues;

        /// <summary> Built from its issues and issues. </summary>
        public ValidationReport(List<RuleIssue> ruleIssues, List<GraphIssue> graphIssues)
        {
            RuleIssues = ruleIssues;
            GraphIssues = graphIssues;
        }

        /// <summary> True when nothing was reported at all. </summary>
        public bool IsValid => Count == 0;

        /// <summary> How many findings there are altogether. </summary>
        public int Count => (RuleIssues?.Count ?? 0) + (GraphIssues?.Count ?? 0);

        /// <summary> Whether anything found makes the level unplayable, as opposed to merely wrong
        /// or untidy. This is the question a player's game asks; an editor wants the full report. </summary>
        public bool HasErrors =>
            (RuleIssues?.Any(issue => issue.Rule.Group == RuleGroup.Error) ?? false)
            || (GraphIssues?.Any(issue => issue.Group == RuleGroup.Error) ?? false);

        /// <summary> One line, for a log. </summary>
        public override string ToString()
        {
            if (IsValid) return "Valid";
            return $"{RuleIssues?.Count ?? 0} rule issue(s), {GraphIssues?.Count ?? 0} graph issue(s)";
        }
    }
}
