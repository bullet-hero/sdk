using System;

namespace BH.SDK.Rules.Attributes
{
    // Marks a member deliberately born null; the counterpart of RuleNotNull. Suppresses only the null
    // case - once a value is present every rule applies as before. When to be nullable: docs/NAMING.md.

    /// <summary> Null is a legal value for this property, so the other rules on it skip an absent
    /// value instead of reporting it. </summary>
    [AttributeUsage(BaseRuleAttribute.PropertyTarget)]
    public sealed class RuleOptionalAttribute : Attribute
    {
    }
}
