using System.Reflection;

namespace BH.SDK.Rules.Attributes
{
    /// <summary>
    /// A rule about one property's value: can this rule sit on this property, does the value satisfy
    /// it, and how to repair one that does not. Rules see their surroundings only through
    /// RuleContext - never the raw analysis root - so the same rule works at level scope and inside
    /// a prefab template without knowing which it is in.
    /// </summary>
    public abstract class BasePropertyRuleAttribute : BaseRuleAttribute
    {
        /// <summary> Whether this rule can sit on that property at all - a DECLARATION question, asked once per type. </summary>
        protected abstract bool IsValidTypeInternal(PropertyInfo property);

        /// <summary> Whether the value satisfies the rule. </summary>
        protected abstract bool IsValidInternal(object value, RuleContext context);

        /// <summary> Repair the property in place. </summary>
        protected abstract void FixInternal(object target, PropertyInfo property, RuleContext context);

        /// <summary> Guarded entry to the type check. </summary>
        public bool IsValidType(PropertyInfo property)
        {
            return property != null && IsValidTypeInternal(property);
        }

        /// <summary> Guarded entry to the value check; a null value never satisfies a rule. </summary>
        public bool IsValid(object value, RuleContext context)
        {
            if (!HasIsValid) return true; // by default any property is valid
            if (value == null) return false;
            return IsValidInternal(value, context);
        }

        /// <summary> Guarded entry to the repair; a read-only property is left alone. </summary>
        public void Fix(object target, PropertyInfo property, RuleContext context)
        {
            if (!HasFix) return;
            if (target == null || property == null || !property.CanWrite) return;
            FixInternal(target, property, context);
        }
    }
}
