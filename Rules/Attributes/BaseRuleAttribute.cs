using System;

namespace BH.SDK.Rules.Attributes
{
    // Two contracts, because rules answer two questions: a property rule sees ONE value, an object rule
    // sees the whole object (a property rule never receives the sibling it must be compared against).
    // This class holds only what both share.

    /// <summary> Base of every declarative rule, of either kind. </summary>
    public abstract class BaseRuleAttribute : Attribute
    {
        /// <summary> Where an object rule may be written. </summary>
        public const AttributeTargets ClassTarget = AttributeTargets.Class;

        /// <summary> Where a property rule may be written. </summary>
        public const AttributeTargets PropertyTarget = AttributeTargets.Property;

        // Abstract, not "GetType().Name": a default would let a new rule reach a UI unnamed. Same
        // contract as LevelEditorOperation.OperationNameKey in the game.

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
