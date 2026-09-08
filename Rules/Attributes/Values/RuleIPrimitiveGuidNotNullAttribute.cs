using System;
using System.Reflection;
using BH.SDK.Models.Interfaces.Primitives;

namespace BH.SDK.Rules.Attributes
{
    // Generic "is set" check for any primitive-guid struct (PrefabId, ...): value must not be the
    // reserved NullValue (always Guid.Empty by convention). See RuleLevelIdValid for LevelId's own
    // dedicated (context-free but type-specific) equivalent.

    /// <summary> A guid id wrapper that must actually be set, rather than left at the empty guid. </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleIPrimitiveGuidNotNullAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_iprimitive_guid_not_null"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_iprimitive_guid_not_null";

        /// <summary> Applies to guid id wrappers. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(IPrimitiveGuid).IsAssignableFrom(property.PropertyType);

        /// <summary> Passes on anything but the empty guid. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
            => value is IPrimitiveGuid primitive && primitive.Value != Guid.Empty;

        /// <summary> Mints a fresh id, since an unset one names nothing. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            if (property.GetValue(target) is not IPrimitiveGuid primitive) return;
            if (primitive.Value != Guid.Empty) return;

            var fixedValue = Activator.CreateInstance(property.PropertyType, Guid.NewGuid());
            property.SetValue(target, fixedValue);
        }
    }
}
