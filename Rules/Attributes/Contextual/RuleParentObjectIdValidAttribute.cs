using System;
using System.Reflection;
using BH.SDK.Models.Primitives;

namespace BH.SDK.Rules.Attributes
{
    // Validates a ParentObjectId: also allows Null and the reserved targets legal for the scope
    // (PrefabRoot only inside a template; Camera/LocalPlayer unreachable from it). With no scope
    // resolved every reserved target is accepted.

    /// <summary> A parent reference must be a user-space id, Null, or a reserved target legal in the
    /// current scope. </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleParentObjectIdValidAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_parent_object_id_valid"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_parent_object_id_valid";

        /// <summary> Applies to object id properties. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(ObjectId).IsAssignableFrom(property.PropertyType);

        /// <summary> Passes on an id this scope may actually be parented to - which excludes the level-runtime parents inside a prefab template. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
            => value is ObjectId objectId && IsAllowed(objectId, context);

        /// <summary> Unsets the reference rather than guessing a parent. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            if (property.GetValue(target) is not ObjectId objectId) return;

            if (!IsAllowed(objectId, context))
                property.SetValue(target, ObjectId.Null);
        }

        private static bool IsAllowed(ObjectId objectId, RuleContext context)
        {
            if (!objectId.IsValidParent()) return false;
            if (context is not { HasScope: true }) return true;

            if (objectId == ObjectId.PrefabRoot) return context.IsPrefabScope;
            if (objectId == ObjectId.Camera) return !context.IsPrefabScope;
            if (objectId == ObjectId.LocalPlayer) return !context.IsPrefabScope;

            return true;
        }
    }
}
