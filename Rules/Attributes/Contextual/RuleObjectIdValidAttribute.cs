using System;
using System.Reflection;
using BH.SDK.Models.Primitives;

namespace BH.SDK.Rules.Attributes
{
    // Validates a "regular" ObjectId reference (RectObject.ObjectId, ObjectIdModification.Prev/Next
    // ObjectId, ...): must be in the user-space range (ObjectId.IsValid(), value >= MinLevelValue).
    // The reserved negative ids are parent targets only - an object may attach to the camera, it
    // may not BE the camera - so use RuleParentObjectIdValid for properties that reference those.
    //
    // Scope-independent by design: an id is judged the same inside a prefab template as at level
    // scope. What still needs the graph pass is everything relational - uniqueness within a scope,
    // and whether the id is actually the key it is filed under.

    /// <summary> An object's own identity must be a user-space id. </summary>
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
            => value is ObjectId objectId && objectId.IsValid();

        /// <summary> Writes the first id of that range. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            if (property.GetValue(target) is not ObjectId objectId) return;

            if (!objectId.IsValid())
                property.SetValue(target, ObjectId.MinLevel);
        }
    }
}
