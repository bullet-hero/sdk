using System;
using System.Reflection;
using BH.SDK.Models.Interfaces.Primitives;

namespace BH.SDK.Rules.Attributes
{
    // Generic max-value check for any primitive-int struct (AudioId, TypedResourceId, ...)

    /// <summary> An int id wrapper whose value has a ceiling - which is how one id range is told from another. </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleIPrimitiveIntMaxAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_iprimitive_int_max"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_iprimitive_int_max";

        // always include

        /// <summary> Upper bound. </summary>
        public int Max { get; set; }

        /// <summary> The bound, as <c>int</c>. </summary>
        public RuleIPrimitiveIntMaxAttribute(int max)
        {
            Max = max;
        }

        /// <summary> Applies to int id wrappers. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(IPrimitiveInt).IsAssignableFrom(property.PropertyType);

        /// <summary> Passes when the id is at or below the bound. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
            => value is IPrimitiveInt primitive && primitive.Value <= Max;

        /// <summary> Clamps it down to the bound. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            if (property.GetValue(target) is not IPrimitiveInt primitive) return;
            if (primitive.Value <= Max) return;

            var fixedValue = Activator.CreateInstance(property.PropertyType, Max);
            property.SetValue(target, fixedValue);
        }
    }
}
