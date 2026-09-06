using System;

namespace BH.SDK.Rules.Attributes
{
    // THE MODEL SAYING "THIS ONE MAY BE ABSENT", and the counterpart of RuleNotNull rather than a
    // weaker version of it. Every property rule reports a null as a violation - that is the safety
    // net for a forgotten RuleNotNull, and a fixture per rule pins it - which is exactly right until
    // a member is deliberately born null, and then a bound starts reporting an ABSENT value as out
    // of range. Without a marker, "no RuleNotNull here" is indistinguishable from "somebody forgot
    // one", so the permission has to be written down.
    //
    // It suppresses nothing but the null case: the moment the property holds a value, every rule on
    // it applies exactly as before.
    //
    // See docs/NAMING.md for when a member should be nullable at all - the short version is that
    // null is for a third state, or for a default block big enough that not writing it pays.

    /// <summary> Null is a legal value for this property, so the other rules on it skip an absent
    /// value instead of reporting it. </summary>
    [AttributeUsage(BaseRuleAttribute.PropertyTarget)]
    public sealed class RuleOptionalAttribute : Attribute
    {
    }
}
