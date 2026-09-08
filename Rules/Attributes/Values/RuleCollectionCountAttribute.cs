using System;
using System.Collections;
using System.Reflection;
using BH.SDK.Utils;

namespace BH.SDK.Rules.Attributes
{
    /// <summary> A collection that must hold exactly this many items, repaired by trimming or padding it. </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleCollectionCountAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_collection_count"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_collection_count";

        /// <summary> The exact length required. </summary>
        public int Count { get; set; }

        /// <summary> Takes the exact length required. </summary>
        public RuleCollectionCountAttribute(int count)
        {
            Count = count;
        }

        /// <summary> Applies to any collection. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(ICollection).IsAssignableFrom(property.PropertyType);

        /// <summary> Passes when the collection holds exactly Count items. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
            => value is ICollection col && col.Count == Count;

        /// <summary> Trims or pads it to Count; an array is replaced, since it cannot be resized. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            var value = property.GetValue(target);
            if (value == null) return;

            // Array is tested first because it also implements IList - an IList-first branch swallows
            // every array and then throws on RemoveAt/Add, since an array is fixed-size. Resizing an
            // array means writing a new one back through the property, never mutating it in place.
            if (value is Array array)
            {
                FixArray(array, property, target);
            }
            else if (value is IList list)
            {
                FixList(list, property);
            }
        }

        private void FixList(IList list, PropertyInfo property)
        {
            if (list.Count > Count)
            {
                while (list.Count > Count)
                    list.RemoveAt(list.Count - 1);
            }
            else if (list.Count < Count)
            {
                var elementType = property.PropertyType.GetListGenericParameterOrDefault();
                
                while (list.Count < Count)
                {
                    object newItem = null;
                    if (elementType is { IsValueType: true })
                        newItem = Activator.CreateInstance(elementType);
                    list.Add(newItem);
                }
            }
        }
        private void FixArray(Array array, PropertyInfo property, object target)
        {
            var length = array.Length;
            if (length == Count) return;

            var elementType = property.PropertyType.GetElementType();
            var newArray = Array.CreateInstance(elementType, Count);

            var copyLength = Math.Min(length, Count);
            if (copyLength > 0) Array.Copy(array, newArray, copyLength);
            
            property.SetValue(target, newArray);
        }
    }
}
