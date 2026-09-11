using System;
using System.Reflection;
using BH.SDK.Models.Enums.Values;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Values;
using BH.SDK.Utils;

namespace BH.SDK.Rules.Attributes
{
    /// <summary> An authored string value with a length ceiling, applied to every language it carries. </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleIStringMaxAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_istring_max"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_istring_max";

        // Warning, for RuleStringMax's reason: cosmetic, and repaired only by truncating authored text.

        /// <summary> A warning: the level still plays, but this is not what the author meant. </summary>
        public override RuleGroup Group => RuleGroup.Warning;

        /// <summary> The longest the string may be. </summary>
        public int MaxLength { get; set; }

        /// <summary> Takes the length ceiling. </summary>
        public RuleIStringMaxAttribute(int maxLength)
        {
            MaxLength = maxLength;
        }

        /// <summary> Applies to authored string properties. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(IString).IsAssignableFrom(property.PropertyType);

        /// <summary> Passes when every language's text is within the length ceiling. </summary>
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

        // Both branches go through SurrogateUtils for the reason RuleStringMax does: the ceiling
        // counts code units, an astral character is two of them, and a cut between the halves
        // satisfies the bound with something that is not a character.

        /// <summary> Truncates each of them, so a localized value keeps every language it had. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            var value = property.GetValue(target);
            if (value is not IString str) return;

            switch (str.GetModelType())
            {
                case StringType.Value:
                {
                    var valueValue = (StringValue)value;
                    valueValue.Value = SurrogateUtils.Truncate(valueValue.Value, MaxLength);
                    break;
                }
                case StringType.Localized:
                {
                    var localizedValue = (StringLocalized)value;
                    foreach (var stringLanguage in localizedValue.Strings)
                    {
                        if (stringLanguage.Value.Length > MaxLength)
                            stringLanguage.Value = SurrogateUtils.Truncate(stringLanguage.Value, MaxLength);
                    }

                    break;
                }
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
}
