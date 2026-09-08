using System;

namespace BH.SDK.Rules.Attributes
{
    // Split into two contracts because rules answer two different questions:
    //
    // - BasePropertyRuleAttribute sees ONE value ("is this frame inside the timeline") and is what
    //   almost every rule needs.
    // - BaseObjectRuleAttribute sees the whole object ("is Min below Max", "is one property below
    //   another") and exists because a property rule structurally cannot: it receives the value and
    //   the context, never the sibling it has to be compared against.
    //
    // This class holds only what both share, so RuleIssue and the severity filter can treat any rule
    // uniformly without knowing which kind it is.

    /// <summary> Base of every declarative rule, of either kind. </summary>
    public abstract class BaseRuleAttribute : Attribute
    {
        /// <summary> Where an object rule may be written. </summary>
        public const AttributeTargets ClassTarget = AttributeTargets.Class;

        /// <summary> Where a property rule may be written. </summary>
        public const AttributeTargets PropertyTarget = AttributeTargets.Property;

        // Abstract rather than "virtual => GetType().Name" on purpose. A consumer that shows issues
        // to a person needs a name that survives a rename and can be translated, and a default would
        // let a new rule reach a UI as an unnamed issue nobody notices until a player reports it.
        // Same contract, and the same reason, as LevelEditorOperation.OperationNameKey in the game.

        /// <summary> Stable identifier of this rule, shaped as a localization key
        /// ("rule_" + the type name without its Rule prefix/Attribute suffix, in snake_case). </summary>
        public abstract string RuleNameKey { get; }

        /// <summary> How badly a violation breaks the level: Error means unplayable, Warning means
        /// playable but wrong, Advice means cosmetic. Drives RuleAnalyzerSettings' severity filter. </summary>
        public virtual RuleGroup Group => RuleGroup.Error;

        /// <summary> False for a rule that only carries a repair, so nothing is ever reported for it. </summary>
        public virtual bool HasIsValid => true;

        /// <summary> False for a rule whose violation is a content decision the author has to make. </summary>
        public virtual bool HasFix => true;
    }
}
