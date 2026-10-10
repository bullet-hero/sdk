using BH.SDK.Publishing;

namespace BH.SDK.Validations
{
    // THREE PASSES, THREE QUESTIONS, none subsuming another: values in range, objects agreeing, may this go
    // to strangers. Only the content half is ever repaired - the others are content decisions.
    // Who reads which part: BH.SDK/Docs/VALIDATIONS.md.

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
        /// Publishable with no human involved: every publish input present and the content half CLEAN,
        /// since a warning means the level does not play as written.
        /// </summary>
        public bool IsReady => Publish.IsReady && Content.IsValid;

        /// <summary> One line, for a log. </summary>
        public override string ToString() => $"{Content}, {Publish.Count} publish issue(s)";
    }
}
