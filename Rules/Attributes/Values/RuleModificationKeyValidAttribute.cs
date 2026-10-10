using System;
using System.Reflection;
using BH.SDK.Models;
using BH.SDK.Models.Primitives;
using BH.SDK.Utils;

namespace BH.SDK.Rules.Attributes
{
    // A struct dictionary key is in the analyzer's blind spot, so it needs this. Checks the field id is
    // registered and an index legal; NOT that the field exists on the addressed object's type (graph
    // pass: needs the placement and its template).

    /// <summary>
    /// A modification key must address a real user-space object (or the template root, for a field
    /// the template owns) and a registered field, with an index only on a collection field.
    /// </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleModificationKeyValidAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_modification_key_valid"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_modification_key_valid";

        // Warning, not Error, and its own header already says why: a key that does not resolve
        // degrades into "the override silently does not apply". The placement still materializes
        // and the level still plays - it plays the template's value instead of the author's.

        /// <summary> A warning: the level still plays, but this is not what the author meant. </summary>
        public override RuleGroup Group => RuleGroup.Warning;

        // Nothing here is repairable, so no fix is declared: any repair guesses which field or object
        // the author meant. Dropping the entry belongs to whoever owns the dictionary.

        /// <summary> No automatic repair - see the note above. </summary>
        public override bool HasFix => false;

        /// <summary> Applies to prefab-override key properties. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(ModificationKey).IsAssignableFrom(property.PropertyType);

        /// <summary> Passes when the object is addressable, the field is registered, and any index is legal. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
        {
            if (value is not ModificationKey key) return false;
            if (!IsAddressableObject(key)) return false;
            if (!ModificationTable.Exists(key.Field)) return false;

            return key.Index == ModificationKey.WholeField
                   || (key.Index >= 0 && ModificationTable.IsCollection(key.Field));
        }

        /// <summary> Nothing - see <see cref="HasFix"/>. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
        }

        // Two kinds of object are addressable: a user-space id (any field of the copy) and PrefabRoot
        // (only ModificationFields.IsPrefabRootField). Camera and avatar ids never are.
        // Why PlacementDuration is the one placement field: BH.SDK/Docs/RULES.md.
        private static bool IsAddressableObject(ModificationKey key)
        {
            if (key.ObjectId.IsValid()) return true;

            return key.ObjectId == ObjectId.PrefabRoot
                   && ModificationFields.IsPrefabRootField(key.Field);
        }
    }
}