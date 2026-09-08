using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace BH.SDK.Rules.Attributes
{
    // Advice, not Error, and deliberately so: the format does NOT require sorted tracks. Frames must
    // be unique (RuleCollectionUnique), order is the consumer's problem - Unity's LevelStateBuilder
    // sorts once at load, and a hand-written or generated file is perfectly legal unsorted.
    //
    // It still earns its place: an editor that shows a track in file order, or a diff between two
    // saves, reads much better when the file is sorted, and sorting is free at save time. So this
    // reports "worth tidying" rather than "broken", and only surfaces when a consumer asks for
    // Advice-level findings.

    /// <summary>
    /// Elements should be in ascending order of the named property. Advisory - unsorted data is
    /// valid, just harder to read.
    /// </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleCollectionSortedAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_collection_sorted"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_collection_sorted";

        /// <summary> Which property of an item decides the answer; empty means the item itself does. </summary>
        public string ItemPropertyName { get; set; }

        /// <summary> Takes which property of an item decides the answer. </summary>
        public RuleCollectionSortedAttribute(string itemPropertyName)
        {
            ItemPropertyName = itemPropertyName;
        }

        /// <summary> Advice: nothing is wrong, it could just be tidier. </summary>
        public override RuleGroup Group => RuleGroup.Advice;

        /// <summary> Applies to any collection. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(ICollection).IsAssignableFrom(property.PropertyType);

        /// <summary> Passes when the items are in ascending order of the named key. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
        {
            if (value is not ICollection collection) return false;

            PropertyInfo keyProperty = null;
            IComparable previous = null;

            foreach (var item in collection)
            {
                if (item == null) continue;

                keyProperty ??= item.GetType().GetProperty(ItemPropertyName);
                if (keyProperty == null) return false;

                if (keyProperty.GetValue(item) is not IComparable key) return false;
                if (previous != null && previous.CompareTo(key) > 0) return false;

                previous = key;
            }
            return true;
        }

        /// <summary> Sorts them by that key, keeping every item. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            if (property.GetValue(target) is not IList list || list.IsReadOnly) return;
            if (list.Count < 2) return;

            var keyProperty = FindKeyProperty(list);
            if (keyProperty == null) return;

            var items = new List<object>(list.Count);
            foreach (var item in list) items.Add(item);

            items.Sort((left, right) => Compare(keyProperty, left, right));

            for (var i = 0; i < items.Count; i++) list[i] = items[i];
        }

        private PropertyInfo FindKeyProperty(IEnumerable list)
        {
            foreach (var item in list)
            {
                if (item != null) return item.GetType().GetProperty(ItemPropertyName);
            }
            return null;
        }

        // Nulls sort first so they stay reachable for whatever rule is meant to remove them, rather
        // than being buried at the end of the track.
        private static int Compare(PropertyInfo keyProperty, object left, object right)
        {
            if (left == null) return right == null ? 0 : -1;
            if (right == null) return 1;

            var leftKey = keyProperty.GetValue(left) as IComparable;
            var rightKey = keyProperty.GetValue(right);

            return leftKey?.CompareTo(rightKey) ?? 0;
        }
    }
}
