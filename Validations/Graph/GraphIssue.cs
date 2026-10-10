using BH.SDK.Rules;

namespace BH.SDK.Validations.Graph
{
    // Deliberately NOT a RuleIssue: a RulePath ends at one value, a graph finding has none to point at
    // or write into. It carries no repair and has no machinery for one - every fix is a content decision.
    // Why, and where a repair would go instead: BH.SDK/Docs/VALIDATIONS.md.

    /// <summary> One violated cross-object invariant: what broke, how badly, and where. </summary>
    public readonly struct GraphIssue
    {
        /// <summary> Which finding this is. </summary>
        public readonly GraphRule Rule;

        /// <summary> How badly it breaks the level. </summary>
        public readonly RuleGroup Group;

        /// <summary> Human-readable location - a scope plus the ids involved, since there is no
        /// property path to give. </summary>
        public readonly string Path;

        /// <summary> What to tell the author. Graph findings carry no repair - each is a content decision. </summary>
        public readonly string Message;

        /// <summary> Built from its rule, group, path and message. </summary>
        public GraphIssue(GraphRule rule, RuleGroup group, string path, string message)
        {
            Rule = rule;
            Group = group;
            Path = path;
            Message = message;
        }

        /// <summary> One line, for a log. </summary>
        public override string ToString() => $"Graph issue, Rule: {Rule}, At: {Path}, {Message}";
    }
}
