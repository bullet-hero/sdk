using System;
using System.Reflection;
using BH.SDK.Models.Primitives;

namespace BH.SDK.Rules.Attributes
{
    // Validates a "regular" ObjectId reference (RectObject.ObjectId, ObjectIdModification.Prev/Next
    // ObjectId, ...): must be in the user-space range (ObjectId.IsValid(), value >= MinLevelValue).
    // Two of the three reserved negative ids are parent targets only - an object may attach to the
    // camera, it may not BE the camera - so use RuleParentObjectIdValid for properties referencing
    // those.
    //
    // PREFABROOT IS THE ONE EXCEPTION, AND IT IS NOT A LENIENCY GAP: Prefab.Root is a real object
    // whose own identity IS ObjectId.PrefabRoot, which is what makes an inner object's
    // ParentObjectId = PrefabRoot an ordinary reference rather than a sentinel resolved by special
    // case. So this rule is contextual after all, and exactly as narrowly as RuleParentObjectIdValid
    // is: PrefabRoot as an identity is legal inside a template and nowhere else. What stops an
    // INNER object from claiming it is Prefab.Objects' own RuleDictionaryKeyMatches - an id has to
    // be the key it is filed under, and nothing mints -3 as a key.
    //
    // With no scope resolved (a standalone value model) the reserved id falls back to being
    // accepted, the same fallback RuleParentObjectIdValid takes rather than inventing a scope. What
    // still needs the graph pass is everything relational - uniqueness within a scope, and whether
    // the id is actually the key it is filed under.

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
