using System;
using System.Reflection;

namespace BH.SDK.Rules.Attributes
{
    // Single-choice enum FIELDS only. Not the polymorphism discriminators (a converter throws on those)
    // and not [Flags] enums (use RuleEnumFlagsValid; do not loosen this one to cover them).

    /// <summary>
    /// An enum field must hold a value the enum actually declares. Fix falls back to DefaultValue,
    /// or to the enum's own zero value when that is declared, or to its first declared value.
    /// </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleEnumValidAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_enum_valid"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_enum_valid";

        /// <summary> What a repair writes instead of the nearer bound, when a bound is not the right answer. </summary>
        public object DefaultValue { get; set; }

        /// <summary> Takes nothing; the defaults apply. </summary>
        public RuleEnumValidAttribute() { }

        /// <summary> What a repair writes, as <c>object</c>. </summary>
        public RuleEnumValidAttribute(object defaultValue)
        {
            DefaultValue = defaultValue;
        }

        /// <summary> Applies to enum properties. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => property.PropertyType.IsEnum;

        /// <summary> Passes on a value the enum actually declares - which is what a retired member's number no longer is. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
        {
            var type = value.GetType();
            return type.IsEnum && Enum.IsDefined(type, value);
        }

        /// <summary> Writes DefaultValue, or the enum's own first member. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            var type = property.PropertyType;
            if (!type.IsEnum) return;

            var value = property.GetValue(target);
            if (value != null && Enum.IsDefined(type, value)) return;

            property.SetValue(target, ResolveFallback(type));
        }

        private object ResolveFallback(Type type)
        {
            if (DefaultValue != null && DefaultValue.GetType() == type && Enum.IsDefined(type, DefaultValue))
                return DefaultValue;

            // Zero is the conventional "unset/first" value and is what a freshly-constructed model
            // holds, so prefer it when the enum declares it.
            var zero = Activator.CreateInstance(type);
            if (Enum.IsDefined(type, zero)) return zero;

            var declared = Enum.GetValues(type);
            return declared.Length > 0 ? declared.GetValue(0) : zero;
        }
    }
}
