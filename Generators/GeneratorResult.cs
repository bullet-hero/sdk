using System;
using BH.SDK.Models.Primitives;
using BH.SDK.Validations;

namespace BH.SDK.Generators
{
    /// <summary>
    /// What one scope-generator run produced: what to select afterwards, what to undo it with, and
    /// optionally what validation thought of the result.
    /// </summary>
    public readonly struct GeneratorResult
    {
        /// <summary> Objects the run created, in creation order - a host selects these so the author
        /// can immediately move what they just generated. </summary>
        public readonly ObjectId[] CreatedIds;

        /// <summary> The journal that undoes and redoes this run. </summary>
        public readonly GeneratorChangeLog Log;

        // Validation is not run here. It costs a full reflective walk of the touched roots, most
        // runs don't need it, and the decision of what to DO about findings is a host policy
        // (ValidationFacade's own header makes the same point). A host that wants a report runs
        // ValidationFacade itself and calls WithReport.

        /// <summary> Findings for this run, if the host asked for any. Default (empty) otherwise. </summary>
        public readonly ValidationReport Report;

        /// <summary> What a run produced, with no validation report. </summary>
        public GeneratorResult(ObjectId[] createdIds, GeneratorChangeLog log)
            : this(createdIds, log, default)
        {
        }
        /// <summary> The same, with one. </summary>
        public GeneratorResult(ObjectId[] createdIds, GeneratorChangeLog log, ValidationReport report)
        {
            CreatedIds = createdIds ?? Array.Empty<ObjectId>();
            Log = log;
            Report = report;
        }

        /// <summary> The same result carrying a report a host ran separately. </summary>
        public GeneratorResult WithReport(ValidationReport report) => new(CreatedIds, Log, report);

        /// <summary> One line, for a log. </summary>
        public override string ToString() => $"{CreatedIds?.Length ?? 0} created, {Log}";
    }
}
