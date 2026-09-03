using System;
using System.Reflection;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;

namespace BH.SDK.Rules.Attributes
{
    [AttributeUsage(PropertyTarget)]
    public class RuleIStringMaxAttribute : BasePropertyRuleAttribute
    {
        public override string RuleNameKey => "rule_istring_max";

        // Warning, for RuleStringMax's reason: cosmetic, and repaired only by truncating authored text.
        public override RuleGroup Group => RuleGroup.Warning;

        public int MaxLength { get; set; }

        public RuleIStringMaxAttribute(int maxLength)
        {
            MaxLength = maxLength;
        }

        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(IString).IsAssignableFrom(property.PropertyType);
        
        protected override bool IsValidInternal(object value, RuleContext context)
        {
            if (value is not IString str) return false;

            switch (str.GetModelType())
            {
                case StringType.Value:
                {
                    var valueValue = (StringValue)value;
                    return valueValue.Value.Length <= MaxLength;
                }
                case StringType.Localized:
                {
                    var localizedValue = (StringLocalized)value;
                    foreach (var stringLanguage in localizedValue.Strings)
                    {
                        if (stringLanguage.Value.Length > MaxLength) 
                            return false;
                    }
                    return true;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }

        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            var value = property.GetValue(target);
            if (value is not IString str) return;
            
            switch (str.GetModelType())
            {
                case StringType.Value:
                {
                    var valueValue = (StringValue)value;
                    valueValue.Value = valueValue.Value[..MaxLength];
                    break;
                }
                case StringType.Localized:
                {
                    var localizedValue = (StringLocalized)value;
                    foreach (var stringLanguage in localizedValue.Strings)
                    {
                        if (stringLanguage.Value.Length > MaxLength) 
                            stringLanguage.Value = stringLanguage.Value[..MaxLength];
                    }
                    break;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}
