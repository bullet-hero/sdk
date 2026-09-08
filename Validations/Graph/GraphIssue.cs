using BH.SDK.Rules;

namespace BH.SDK.Validations.Graph
{
    // Deliberately NOT a RuleIssue. A RuleIssue is addressed by a RulePath - a chain of properties
    // and collection keys ending at one value - which is exactly what a graph finding is not: "these
    // two objects share an id", "this chain loops back on itself" have no single property to point
    // at, and no property to write a repair into. Forcing them into that shape would produce issues
    // whose Fix could not run and whose path lied about where the problem was.
    //
    // A finding here therefore carries no repair at all, and the type has no machinery for one: which
    // of two colliding objects keeps its id, where a broken chain reattaches, are content decisions,
    // and guessing would silently rewrite the author's level. The one relationship that did have a
    // uniquely determined repair - a child's span reaching outside its parent's - is not a finding
    // any more: the overhang is legal data resolved away on read, and fitting the lifetimes is an
    // edit the author asks for (mod_span_fit). Should a future invariant genuinely need a repair, it
    // belongs there too, as a generator, not as a fix delegate reporting itself as a defect.

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
