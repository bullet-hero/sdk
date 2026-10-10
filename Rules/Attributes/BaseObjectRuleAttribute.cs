using System;

namespace BH.SDK.Rules.Attributes
{
    // Declared on the class: the invariant spans several properties. Run once per object, before its
    // properties, so a finding lands on the object a Fix writes into. Cross-object checks are graph
    // invariants and do not belong here.

    /// <summary>
    /// A rule about a whole object: an invariant between several of its properties, which a
    /// property rule structurally cannot express - it only ever receives one value.
    /// </summary>
    public abstract class BaseObjectRuleAttribute : BaseRuleAttribute
    {
        /// <summary> Whether this rule can sit on that type at all - a DECLARATION question, asked once per type. </summary>
        protected abstract bool IsValidTypeInternal(Type type);

        /// <summary> Whether the whole object satisfies the rule. </summary>
        protected abstract bool IsValidInternal(object target, RuleContext context);

        /// <summary> Repair the object in place. </summary>
        protected abstract void FixInternal(object target, RuleContext context);

        /// <summary> Guarded entry to the type check. </summary>
        public bool IsValidType(Type type)
        {
            return type != null && IsValidTypeInternal(type);
        }

        /// <summary> Guarded entry to the object check. </summary>
        public bool IsValid(object target, RuleContext context)
        {
            if (!HasIsValid) return true;
            if (target == null) return false;
            return IsValidInternal(target, context);
        }

        /// <summary> Guarded entry to the repair. </summary>
        public void Fix(object target, RuleContext context)
        {
            if (!HasFix) return;
            if (target == null) return;
            FixInternal(target, context);
        }
    }
}
