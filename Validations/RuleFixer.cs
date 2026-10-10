using System.Collections.Generic;

namespace BH.SDK.Validations
{
    /// <summary> Applies the repair each finding carries, deepest first, and re-analyzes until nothing new
    /// appears - a repair can legitimately create the next violation. </summary>
    public class RuleFixer
    {
        // The root is no longer a parameter: every issue carries the context it was found in, which
        // is both the root and the scope-local bounds a repair has to respect.

        /// <summary> Repairs every finding, deepest first, re-analyzing until nothing new appears. </summary>
        public void Fix(List<RuleIssue> issues, RuleFixerSettings settings)
        {
            // Cat.Meow("Fix");

            // Reverse order: a fix can invalidate deeper issues (especially with analyzeAllRecursiveRules).
            // Each issue carries its own context (a fix in a template clamps against that template's
            // timeline) and whether it addresses a property or a whole object.
            for (var i = issues.Count - 1; i >= 0; i--)
            {
                issues[i].ApplyFix();
            }
        }

        /// <summary>
        /// Analyze and repair repeatedly until nothing is reported or the pass budget runs out.
        /// Returns whatever is still reported - empty means fully repaired.
        /// </summary>
        public List<RuleIssue> FixUntilStable(RuleAnalyzer analyzer, object root,
            RuleAnalyzerSettings analyzerSettings, RuleFixerSettings settings)
        {
            var issues = analyzer.Analyze(root, analyzerSettings);

            for (var pass = 0; pass < settings.maxPasses && issues.Count > 0; pass++)
            {
                Fix(issues, settings);
                issues = analyzer.Analyze(root, analyzerSettings);
            }
            return issues;
        }
    }
}
