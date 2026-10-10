using System;
using System.Reflection;
using BH.SDK.Models.Primitives;

namespace BH.SDK.Rules.Attributes
{
    // Validates a regular ObjectId reference: user-space range only. PrefabRoot is legal as an identity
    // inside a template and nowhere else; with no scope resolved the reserved id is accepted.
    // Why: BH.SDK/Docs/RULES.md, "RuleObjectIdValid".

    /// <summary> An object's own identity must be a user-space id, or the reserved template root
    /// inside a template. </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleObjectIdValidAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_object_id_valid"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_object_id_valid";

        /// <summary> Applies to object id properties. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(ObjectId).IsAssignableFrom(property.PropertyType);

        /// <summary> Passes on an id inside the range this scope hands out. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
            => value is ObjectId objectId && IsAllowed(objectId, context);

        /// <summary> Writes the first id of that range. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            if (property.GetValue(target) is not ObjectId objectId) return;

            if (!IsAllowed(objectId, context))
                property.SetValue(target, ObjectId.MinLevel);
        }

        private static bool IsAllowed(ObjectId objectId, RuleContext context)
        {
            if (objectId.IsValid()) return true;
            if (objectId != ObjectId.PrefabRoot) return false;

            return context is not { HasScope: true } || context.IsPrefabScope;
        }
    }
}
