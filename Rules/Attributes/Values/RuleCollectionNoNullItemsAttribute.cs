using System;
using System.Collections;
using System.Reflection;

namespace BH.SDK.Rules.Attributes
{
    // A null element is invisible to the analyzer - it walks collections item by item and returns
    // immediately on null - so a collection can be reported clean while holding entries nothing can
    // use. Worse, RuleCollectionCount's own Fix pads reference-typed collections with nulls to reach
    // its target, which satisfies the count rule and creates exactly this state.
    //
    // The one place it matters most is ThemeData.Matrix: 64 colour slots addressed by index, where a
    // null slot means every ThemeRef pointing at it resolves to nothing.

    /// <summary> No element of the collection may be null. </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleCollectionNoNullItemsAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_collection_no_null_items"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_collection_no_null_items";

        /// <summary> Applies to any collection. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(ICollection).IsAssignableFrom(property.PropertyType);

        /// <summary> Passes when no entry is null. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
        {
            if (value is not ICollection collection) return false;

            foreach (var item in collection)
            {
                if (item == null) return false;
            }
            return true;
        }

        // Removing is the only repair that cannot be wrong. Replacing a null with a fresh default
        // would invent content the author never wrote - and for an index-addressed collection like
        // ThemeData.Matrix it would silently shift every slot after it, so the count rule that
        // pairs with this one is what restores the length afterwards.

        /// <summary> Removes the null entries, back to front so the indices stay valid. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            if (property.GetValue(target) is not IList list || list.IsReadOnly) return;

            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] == null) list.RemoveAt(i);
            }
        }
    }
}
