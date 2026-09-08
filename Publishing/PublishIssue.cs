using BH.SDK.Rules;

namespace BH.SDK.Publishing
{
    // Shaped like GraphIssue and for the same reason: there is no single property to point at or to
    // repair. "This resource has no metadata record" spans two files, and the repair - stating who
    // made the work and under what terms - is knowledge only the author has.
    //
    // It reuses RuleGroup rather than defining a severity of its own, because the two levels that
    // matter here already exist there and carry exactly the meaning needed: Error is refusal, and
    // Warning is "publish, but a human has to look" - the split that lets one analyzer serve both
    // the automatic check in the client and the moderation queue on a server. Advice is neither and
    // never gates anything.

    /// <summary> One reason a level is not ready to be published, and how badly. </summary>
    public readonly struct PublishIssue
    {
        /// <summary> Which finding this is. </summary>
        public readonly PublishRule Rule;

        /// <summary> Whether it blocks publishing or only asks to be looked at. </summary>
        public readonly RuleGroup Group;

        /// <summary> Human-readable location - which resource record, since there is no property
        /// path to give. </summary>
        public readonly string Path;

        /// <summary> What to tell the author. </summary>
        public readonly string Message;

        /// <summary> Built from its rule, group, path and message. </summary>
        public PublishIssue(PublishRule rule, RuleGroup group, string path, string message)
        {
            Rule = rule;
            Group = group;
            Path = path;
            Message = message;
        }

        /// <summary> One line, for a log. </summary>
        public override string ToString() => $"Publish issue, Rule: {Rule}, At: {Path}, {Message}";
    }
}
