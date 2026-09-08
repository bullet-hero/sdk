using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace BH.SDK.Rules.Attributes
{
    /// <summary> A collection whose items must not repeat - either whole, or by one named property of each. </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleCollectionUniqueAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_collection_unique"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_collection_unique";

        /// <summary> Which property of an item decides identity; empty means the item itself does. </summary>
        public string ItemPropertyName { get; set; }

        /// <summary> Takes nothing; the defaults apply. </summary>
        public RuleCollectionUniqueAttribute()
        {
            ItemPropertyName = string.Empty;
        }
        /// <summary> Takes which property of an item decides the answer. </summary>
        public RuleCollectionUniqueAttribute(string itemPropertyName)
        {
            ItemPropertyName = itemPropertyName;
        }

        /// <summary> Applies to any collection. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(ICollection).IsAssignableFrom(property.PropertyType);
        
        /// <summary> Passes when no two items share the value uniqueness is judged by. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
        {
            if (value is not ICollection collection) return false;

            var set = new HashSet<object>();
            PropertyInfo propertyInfo = null;

            foreach (var item in collection)
            {
                if (item == null) continue;

                object key;
                if (!string.IsNullOrEmpty(ItemPropertyName))
                {
                    if (propertyInfo == null)
                    {
                        propertyInfo = item.GetType().GetProperty(ItemPropertyName);
                        if (propertyInfo == null) return false;
                    }
                    
                    key = propertyInfo.GetValue(item);
                    if (key == null) return false;
                }
                else
                {
                    key = item;
                }

                if (!set.Add(key)) return false;
            }
            return true;
        }

        /// <summary> Drops the later duplicates, back to front so the indices stay valid. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            var value = property.GetValue(target);
            if (value is not IList list || list.IsReadOnly) return;

            var set = new HashSet<object>();
            PropertyInfo propertyInfo = null;
            
            for (var i = list.Count - 1; i >= 0; i--)
            {
                var item = list[i];
                if (item == null)
                {
                    list.RemoveAt(i);
                    continue;
                }

                object key;
                if (!string.IsNullOrEmpty(ItemPropertyName))
                {
                    if (propertyInfo == null)
                    {
                        propertyInfo = item.GetType().GetProperty(ItemPropertyName);
                        if (propertyInfo == null) return;
                    }
                    
                    key = propertyInfo.GetValue(item);
                    if (key == null)
                    {
                        list.RemoveAt(i);
                        continue;
                    }
                }
                else
                {
                    key = item;
                }

                if (!set.Add(key))
                    list.RemoveAt(i);
            }
        }
    }
}
