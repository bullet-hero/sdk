using System;
using System.Collections.Generic;
using BH.SDK.Models;
using BH.SDK.Publishing;
using BH.SDK.Validations.Graph;

namespace BH.SDK.Validations
{
    // The entry point a loader, editor or tool can call without knowing validation has two halves. It does
    // not decide what to DO about a report: a game refuses on HasErrors, an editor shows all, an importer
    // repairs first.

    /// <summary>
    /// The single entry point for validating and repairing save-format data.
    /// </summary>
    public class ValidationFacade
    {
        private readonly RuleAnalyzer _analyzer = new();
        private readonly RuleFixer _fixer = new();
        private readonly LevelGraphAnalyzer _graphAnalyzer = new();
        private readonly PublishReadinessAnalyzer _publishAnalyzer = new();

        /// <summary> Validate any aggregate root - Level, LevelMeta, UserSettings, a standalone
        /// Prefab or EffectData. The graph pass runs only for a Level, since cross-object invariants
        /// need the whole level to resolve against. </summary>
        public ValidationReport Validate(object root, RuleAnalyzerSettings settings = null)
        {
            settings ??= new RuleAnalyzerSettings();

            var ruleIssues = _analyzer.Analyze(root, settings);
            var graphIssues = root is Level level
                ? _graphAnalyzer.Analyze(level)
                : new List<GraphIssue>();

            return new ValidationReport(ruleIssues, graphIssues);
        }

        /// <summary>
        /// Repair what can be repaired, then report what is left. Graph findings are never repaired: every
        /// fix for one is a content decision.
        /// </summary>
        public ValidationReport ValidateAndFix(object root,
            RuleAnalyzerSettings analyzerSettings = null, RuleFixerSettings fixerSettings = null)
        {
            analyzerSettings ??= new RuleAnalyzerSettings();
            fixerSettings ??= new RuleFixerSettings();

            var ruleIssues = _fixer.FixUntilStable(_analyzer, root, analyzerSettings, fixerSettings);

            // The graph pass runs AFTER repairs, not before: a repair can create a graph violation
            // (giving an unset id a value can collide it with an existing one), so the report has to
            // describe the level as it now stands, not as it arrived.
            var graphIssues = root is Level level
                ? _graphAnalyzer.Analyze(level)
                : new List<GraphIssue>();

            return new ValidationReport(ruleIssues, graphIssues);
        }

        // One call, not three: the metadata is its own aggregate root, so validating only the level skips
        // every LevelMeta rule. The level is optional (a catalogue grades metadata alone). Nothing is
        // repaired.

        /// <summary>
        /// Everything all three passes think of a level about to be published: the declarative
        /// rules and the graph over both the level and its metadata, plus one service's own
        /// conditions. <paramref name="level"/> may be null to grade metadata alone.
        /// </summary>
        public PublishValidationReport ValidateForPublish(LevelMeta meta, PublishProfile profile,
            Level level = null, DateTime now = default, PublishPayload payload = null,
            RuleAnalyzerSettings settings = null)
        {
            if (meta == null) throw new ArgumentNullException(nameof(meta));
            if (profile == null) throw new ArgumentNullException(nameof(profile));

            settings ??= new RuleAnalyzerSettings();

            var ruleIssues = _analyzer.Analyze(meta, settings);
            var graphIssues = new List<GraphIssue>();

            if (level != null)
            {
                ruleIssues.AddRange(_analyzer.Analyze(level, settings));
                graphIssues = _graphAnalyzer.Analyze(level);
            }

            var content = new ValidationReport(ruleIssues, graphIssues);
            var publish = _publishAnalyzer.Analyze(meta, profile, level, now, payload);

            return new PublishValidationReport(content, publish);
        }
    }
}
