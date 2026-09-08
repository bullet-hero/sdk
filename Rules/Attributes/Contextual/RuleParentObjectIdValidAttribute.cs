using System;
using System.Reflection;
using BH.SDK.Models.Primitives;

namespace BH.SDK.Rules.Attributes
{
    // Validates a ParentObjectId reference (RectObject.ParentObjectId): unlike a regular ObjectId,
    // this also allows Null (no parent, i.e. level space) and the reserved negative targets.
    //
    // Which reserved targets are legal depends on the scope, which is why this rule needs a context
    // at all: PrefabRoot addresses the template's own root and is meaningless at level scope, while
    // Camera and LocalPlayer are level-runtime objects a template's inner object cannot reach. With
    // no scope resolved (a standalone value model), every reserved target is accepted - the rule
    // falls back to the plain range check rather than inventing a scope it does not have.

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
