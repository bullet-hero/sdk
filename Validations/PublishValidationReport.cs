using BH.SDK.Publishing;

namespace BH.SDK.Validations
{
    // THREE PASSES, THREE QUESTIONS, AND NONE OF THEM SUBSUMES ANOTHER - which is exactly why they
    // are carried side by side here rather than merged into one list. RuleAnalyzer asks whether a
    // value is in range. LevelGraphAnalyzer asks whether the objects agree with each other.
    // PublishReadinessAnalyzer asks whether this may be handed to strangers, which nothing in the
    // file can answer alone - only the file plus a service's policy.
    //
    // A level can be flawless content and unpublishable (nobody credited the music), or ready to
    // publish and broken (every licence in order, a NaN in a position). An editor shows all three; a
    // client blocks an upload on Publish.HasErrors; a server puts NeedsManualReview in a queue.
    //
    // ONLY THE CONTENT HALF IS EVER REPAIRED. Graph findings and publish findings are both content
    // decisions - which of two colliding ids survives, whose name goes in the credits - and guessing
    // would be the analyzer inventing the very paperwork it exists to demand.

    /// <summary> Everything all three validation passes found about one level. </summary>
    public readonly struct PublishValidationReport
    {
        /// <summary> The declarative and graph halves, over the level and its metadata. </summary>
        public readonly ValidationReport Content;

        /// <summary> What publishing itself asks for, on top of the level being valid. </summary>
        public readonly PublishReadinessReport Publish;

        /// <summary> Built from its content and publish. </summary>
        public PublishValidationReport(ValidationReport content, PublishReadinessReport publish)
        {
            Content = content;
            Publish = publish;
        }

        /// <summary> How many findings there are altogether. </summary>
        public int Count => Content.Count + Publish.Count;

        /// <summary> Anything that stops this being published as it stands, from either half. </summary>
        public bool HasErrors => Content.HasErrors || Publish.HasErrors;

        /// <summary>
        /// Publishable with no human involved. Deliberately stricter than <see cref="HasErrors"/>
        /// being false: it also demands that the publish check had every input it needed, since a
        /// metadata-only pass cannot know whether every resource is covered, and that the content
        /// half is CLEAN rather than merely error-free - a warning here means the level does not
        /// play the way its author wrote it, which is not something to hand to strangers silently.
        /// </summary>
        public bool IsReady => Publish.IsReady && Content.IsValid;

        /// <summary> One line, for a log. </summary>
        public override string ToString() => $"{Content}, {Publish.Count} publish issue(s)";
    }
}
