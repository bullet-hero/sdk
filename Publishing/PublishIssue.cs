using System;
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
    //
    // THE FACTS TRAVEL AS DATA, NOT ONLY INSIDE THE SENTENCE. Message is English prose for a log and
    // a server; a client that shows the finding in another language needs the same facts - which
    // resource, which license, what the service accepts instead - as values it can word itself. An
    // enum stays an enum there, so the caller names it through its own string table.

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

        /// <summary> The facts behind the finding, in the order its <see cref="PublishRule"/> member
        /// lists them. A finding about one resource opens with its ResourceType and its name. An enum
        /// (or an array of one) is a value the caller names itself, anything else is already text.
        /// Never null. </summary>
        public readonly object[] Args;

        /// <summary> Built from its rule, group, path, message and facts. </summary>
        public PublishIssue(PublishRule rule, RuleGroup group, string path, string message, params object[] args)
        {
            Rule = rule;
            Group = group;
            Path = path;
            Message = message;
            Args = args ?? Array.Empty<object>();
        }

        /// <summary> One line, for a log. </summary>
        public override string ToString() => $"Publish issue, Rule: {Rule}, At: {Path}, {Message}";
    }
}
