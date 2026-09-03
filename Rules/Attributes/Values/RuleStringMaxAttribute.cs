using System;
using System.Reflection;

namespace BH.SDK.Rules.Attributes
{
    [AttributeUsage(PropertyTarget)]
    public class RuleStringMaxAttribute : BasePropertyRuleAttribute
    {
        public override string RuleNameKey => "rule_string_max";

        // Warning, not Error: a name too long is cosmetic, and the Fix is a TRUNCATION - it destroys
        // authored text to satisfy a bound nothing at playback depends on. Reporting it as fatal
        // would make an author choose between a permanent error and losing what they typed.
        public override RuleGroup Group => RuleGroup.Warning;

        public int MaxLength { get; set; }

        public RuleStringMaxAttribute(int maxLength)
        {
            MaxLength = maxLength;
        }
        
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(string).IsAssignableFrom(property.PropertyType);
        
        protected override bool IsValidInternal(object value, RuleContext context)
            => value is string str && str.Length <= MaxLength;

        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            if (property.PropertyType != typeof(string)) return;
            if (property.GetValue(target) is not string s) return;
            
            if (s.Length > MaxLength)
                property.SetValue(target, s.Substring(0, MaxLength));;
        }
    }
}
