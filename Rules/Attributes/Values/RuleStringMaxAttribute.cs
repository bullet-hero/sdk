using System;
using System.Reflection;

namespace BH.SDK.Rules.Attributes
{
    /// <summary> A plain string with a length ceiling, repaired by truncation. </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleStringMaxAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_string_max"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_string_max";

        // Warning, not Error: a name too long is cosmetic, and the Fix is a TRUNCATION - it destroys
        // authored text to satisfy a bound nothing at playback depends on. Reporting it as fatal
        // would make an author choose between a permanent error and losing what they typed.

        /// <summary> A warning: the level still plays, but this is not what the author meant. </summary>
        public override RuleGroup Group => RuleGroup.Warning;

        /// <summary> The longest the string may be. </summary>
        public int MaxLength { get; set; }

        /// <summary> Takes the length ceiling. </summary>
        public RuleStringMaxAttribute(int maxLength)
        {
            MaxLength = maxLength;
        }
        
        /// <summary> Applies to plain string properties. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(string).IsAssignableFrom(property.PropertyType);
        
        /// <summary> Passes when the string is within its length ceiling. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
            => value is string str && str.Length <= MaxLength;

        /// <summary> Truncates it. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            if (property.PropertyType != typeof(string)) return;
            if (property.GetValue(target) is not string s) return;
            
            if (s.Length > MaxLength)
                property.SetValue(target, s.Substring(0, MaxLength));;
        }
    }
}
