using System;
using System.Reflection;

namespace BH.SDK.Rules.Attributes
{
    // Covers the gap the numeric rules leave open. RuleInRange rejects every non-finite value for
    // free (NaN fails its lower half, the infinities fail one side each), but the one-sided rules do
    // not: NaN sorts below every real number, so RuleMaxValue accepts it, and +Infinity satisfies RuleMinValue.
    // A property with only one bound - or with no numeric rule at all - can therefore hold NaN, and
    // NaN spreads: one poisoned position turns every derived transform, bound and collision result
    // into NaN for the rest of the frame.
    //
    // Prefer a two-sided RuleInRange where a real range exists; reach for this only where the value
    // genuinely has no meaningful bound.

    /// <summary> A floating-point field must hold a real number - not NaN, not an infinity. </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleFiniteNumberAttribute : BasePropertyRuleAttribute
    {
        public override string RuleNameKey => "rule_finite_number";

        public object DefaultValue { get; set; }

        public RuleFiniteNumberAttribute() { }

        public RuleFiniteNumberAttribute(float defaultValue)
        {
            DefaultValue = defaultValue;
        }

        public RuleFiniteNumberAttribute(double defaultValue)
        {
            DefaultValue = defaultValue;
        }

        protected override bool IsValidTypeInternal(PropertyInfo property)
            => property.PropertyType == typeof(float) || property.PropertyType == typeof(double);

        protected override bool IsValidInternal(object value, RuleContext context) => value switch
        {
            float f => !float.IsNaN(f) && !float.IsInfinity(f),
            double d => !double.IsNaN(d) && !double.IsInfinity(d),
            _ => false,
        };

        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            var value = property.GetValue(target);
            if (IsValidInternal(value, context)) return;

            var type = property.PropertyType;
            if (DefaultValue != null && DefaultValue.GetType() == type)
            {
                property.SetValue(target, DefaultValue);
                return;
            }

            // Zero, not the nearest bound: there is no bound here, and zero is the one value every
            // field of this kind can hold without changing what it means.
            property.SetValue(target, type == typeof(float) ? 0f : (object)0d);
        }
    }
}
