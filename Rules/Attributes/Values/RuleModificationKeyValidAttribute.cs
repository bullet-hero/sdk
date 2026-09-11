using System;
using System.Reflection;
using BH.SDK.Models.Primitives;
using BH.SDK.Utils;

namespace BH.SDK.Rules.Attributes
{
    // ModificationKey is a struct used as a dictionary key, which puts it in the analyzer's blind
    // spot twice over: keys are not walked, and a struct can never be a [RuleContainer] anyway. So
    // its members - the template-inner ObjectId being overridden and the field it addresses - were
    // never validated at all, while an address that fails to resolve degrades into "the override
    // silently does not apply" rather than into anything visible.
    //
    // WHAT IT CHECKS CHANGED SHAPE WITH THE ADDRESS. It used to ask whether a dotted path was
    // non-empty and shorter than a ceiling, which was the most a string could be asked without the
    // model in hand. A field id can be asked the real question instead - is this a field of
    // anything at all, and is an index legal on it - because ModificationTable answers both at
    // compile time. What it still does NOT check is whether the field exists on the type of the
    // object that key addresses: that needs the placement, its PrefabId and the template it points
    // at, i.e. the graph pass. The table makes that check POSSIBLE for the first time; it is its
    // own item, not this one.

    /// <summary>
    /// A modification key must address a real user-space object and a registered field, with an
    /// index only where the field is a collection.
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

        // NOTHING HERE IS REPAIRABLE, which is why this rule declares no fix at all rather than
        // offering one that sometimes does nothing. An unregistered field id cannot be turned into
        // a registered one without inventing which field the author meant, and repointing a broken
        // ObjectId would apply the override to the wrong object - silently and plausibly. Dropping
        // the whole entry is the right repair and it belongs to whoever owns the dictionary.
        // RuleReferenceExistsAttribute declines for the same reason.

        /// <summary> No automatic repair - see the note above. </summary>
        public override bool HasFix => false;

        /// <summary> Applies to prefab-override key properties. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(ModificationKey).IsAssignableFrom(property.PropertyType);

        /// <summary> Passes when the object is addressable, the field is registered, and any index is legal. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
        {
            if (value is not ModificationKey key) return false;
            if (!key.ObjectId.IsValid()) return false;
            if (!ModificationTable.Exists(key.Field)) return false;

            return key.Index == ModificationKey.WholeField
                   || (key.Index >= 0 && ModificationTable.IsCollection(key.Field));
        }

        /// <summary> Nothing - see <see cref="HasFix"/>. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
        }
    }
}
