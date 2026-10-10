using System;
using System.Collections;
using System.Reflection;

namespace BH.SDK.Rules.Attributes
{
    // A null element is invisible to the analyzer, and RuleCollectionCount's Fix pads with nulls.
    // Matters most for ThemeData.Matrix (64 index-addressed slots).

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

        // Removing is the only repair that cannot invent content; the paired count rule restores length.

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
