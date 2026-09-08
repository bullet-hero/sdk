using System;
using System.Reflection;
using BH.SDK.Models.Interfaces.Primitives;

namespace BH.SDK.Rules.Attributes
{
    // Generic "is set" check for any primitive-int struct (AudioId, TypedResourceId, ...):
    // value must not be the reserved NullValue (always 0 by convention). Unlike
    // RuleIPrimitiveIntMin/Max, this does not restrict the value to a game-defined or user-defined
    // subrange - use this when a property may reference either range, just not "unset".

    /// <summary> An int id wrapper that must be set - anything but the reserved zero. Says nothing about
    /// which range the id belongs to, unlike the Min/Max pair. </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleIPrimitiveIntNotNullAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_iprimitive_int_not_null"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_iprimitive_int_not_null";

        /// <summary> Applies to int id wrappers. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(IPrimitiveInt).IsAssignableFrom(property.PropertyType);

        /// <summary> Passes on anything but the reserved zero. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
            => value is IPrimitiveInt primitive && primitive.Value != 0;

        /// <summary> Writes the first id of the range this property belongs to. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            if (property.GetValue(target) is not IPrimitiveInt primitive) return;
            if (primitive.Value != 0) return;

            var fixedValue = Activator.CreateInstance(property.PropertyType, 1);
            property.SetValue(target, fixedValue);
        }
    }
}
