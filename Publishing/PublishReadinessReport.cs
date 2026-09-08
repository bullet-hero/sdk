using System.Collections.Generic;
using System.Linq;
using BH.SDK.Rules;

namespace BH.SDK.Publishing
{
    // Carries LevelInspected because the analyzer answers a different question depending on what it
    // was given, and a caller must not mistake one for the other. metadata.json is a separate file
    // precisely so a catalogue can read thousands of them without touching a single level, and that
    // cheap pass genuinely checks most of the policy - but two findings are invisible to it:
    // a resource that has no metadata record at all, and where a resource is fetched from. Both live
    // in level.json. A meta-only report that came back clean therefore means "nothing wrong in what
    // was read", not "ready to publish", and only the flag distinguishes the two.

    /// <summary> Everything one publish check found, and how complete that check was. </summary>
    public readonly struct PublishReadinessReport
    {
        /// <summary> Everything the publish check found. </summary>
        public readonly List<PublishIssue> Issues;

        /// <summary> Whether the level file was available. False means the resource-coverage and
        /// fetch-location checks did not run, whatever the issue list says. </summary>
        public readonly bool LevelInspected;

        /// <summary> Whether measured file sizes were available. False means no size limit was
        /// checked - which is only a gap when the profile actually sets one. </summary>
        public readonly bool PayloadInspected;

        /// <summary> Whether the check had everything the profile needs to reach a verdict at all.
        /// Kept separate from the two flags above because "no sizes were measured" is a gap for a
        /// profile that bounds sizes and irrelevant for one that does not. </summary>
        public readonly bool InputsComplete;

        /// <summary> Which profile produced this report - the same level is ready for one service
        /// and not for another. </summary>
        public readonly string ProfileKey;

        /// <summary> Every member at once, in declaration order. </summary>
        public PublishReadinessReport(List<PublishIssue> issues, bool levelInspected,
            bool payloadInspected, bool inputsComplete, string profileKey)
        {
            Issues = issues;
            LevelInspected = levelInspected;
            PayloadInspected = payloadInspected;
            InputsComplete = inputsComplete;
            ProfileKey = profileKey;
        }

        /// <summary> How many findings there are. </summary>
        public int Count => Issues?.Count ?? 0;

        /// <summary> Something the service refuses outright - what a client blocks the upload on. </summary>
        public bool HasErrors => Issues?.Any(issue => issue.Group == RuleGroup.Error) ?? false;

        /// <summary> Nothing refused, but something a person has to look at - what a server puts in
        /// its moderation queue instead of publishing straight away. </summary>
        public bool NeedsManualReview => Issues?.Any(issue => issue.Group == RuleGroup.Warning) ?? false;

        /// <summary> Publishable with no human involved. Never true for a partial check - a
        /// meta-only pass cannot know whether every resource is covered, and an unmeasured level
        /// cannot be known to fit a service that bounds sizes. </summary>
        public bool IsReady => InputsComplete && !HasErrors && !NeedsManualReview;

        /// <summary> The findings that stop a publish. </summary>
        public IEnumerable<PublishIssue> Errors
            => Issues?.Where(issue => issue.Group == RuleGroup.Error) ?? Enumerable.Empty<PublishIssue>();

        /// <summary> The findings a person has to look at but that block nothing. </summary>
        public IEnumerable<PublishIssue> Reviews
            => Issues?.Where(issue => issue.Group == RuleGroup.Warning) ?? Enumerable.Empty<PublishIssue>();

        /// <summary> One line, for a log. </summary>
        public override string ToString()
        {
            var scope = InputsComplete ? "full" : LevelInspected ? "partial" : "meta-only";
            if (Count == 0) return $"No publish issues ({scope}, profile '{ProfileKey}')";
            return $"{Count} publish issue(s), {Errors.Count()} error(s), " +
                   $"{Reviews.Count()} for review ({scope}, profile '{ProfileKey}')";
        }
    }
}
