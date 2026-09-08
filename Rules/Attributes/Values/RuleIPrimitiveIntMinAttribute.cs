using System;
using System.Reflection;
using BH.SDK.Models.Interfaces.Primitives;

namespace BH.SDK.Rules.Attributes
{
    // Generic min-value check for any primitive-int struct (AudioId, TypedResourceId, ...)

    /// <summary> An int id wrapper whose value has a floor - which is how one id range is told from another. </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleIPrimitiveIntMinAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_iprimitive_int_min"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_iprimitive_int_min";

        // always include

        /// <summary> Lower bound. </summary>
        public int Min { get; set; }

        /// <summary> The bound, as <c>int</c>. </summary>
        public RuleIPrimitiveIntMinAttribute(int min)
        {
            Min = min;
        }

        /// <summary> Applies to int id wrappers. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(IPrimitiveInt).IsAssignableFrom(property.PropertyType);

        /// <summary> Passes when the id is at or above the bound. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
            => value is IPrimitiveInt primitive && primitive.Value >= Min;

        /// <summary> Clamps it up to the bound. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            if (property.GetValue(target) is not IPrimitiveInt primitive) return;
            if (primitive.Value >= Min) return;

            var fixedValue = Activator.CreateInstance(property.PropertyType, Min);
            property.SetValue(target, fixedValue);
        }
    }
}
