using System;
using System.Collections;
using System.Reflection;
using BH.SDK.Utils;

namespace BH.SDK.Rules.Attributes
{
    // The counterpart of RuleCollectionMaxCount, for collections that are meaningless below a
    // certain size: a curve needs two keys to define a segment, a gradient two stops to define a
    // blend, a composite collider one triangle to be a shape at all. Below that every consumer has
    // to invent its own fallback, and they will not agree.
    //
    // Unlike RuleCollectionCount, its Fix never pads with null: an entry that exists but is null is
    // worse than a missing one, since the analyzer walks straight past null elements and everything
    // downstream has to null-check what the rule supposedly guaranteed. If the element type cannot
    // be constructed, the fix declines and the issue stays reported.

    /// <summary> A collection must hold at least MinCount elements. </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleCollectionMinCountAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_collection_min_count"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_collection_min_count";

        /// <summary> The shortest it may be; a repair pads it out. </summary>
        public int MinCount { get; set; }

        /// <summary> Takes the shortest it may be. </summary>
        public RuleCollectionMinCountAttribute(int minCount)
        {
            MinCount = minCount;
        }

        /// <summary> Applies to any collection. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(ICollection).IsAssignableFrom(property.PropertyType);

        /// <summary> Passes when the collection holds at least MinCount items. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
            => value is ICollection collection && collection.Count >= MinCount;

        /// <summary> Pads it out with fresh elements, and gives up when the element type cannot be constructed. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            var value = property.GetValue(target);

            if (value is IList list && !list.IsReadOnly)
            {
                var elementType = property.PropertyType.IsArray
                    ? property.PropertyType.GetElementType()
                    : property.PropertyType.GetListGenericParameterOrDefault();

                FixList(list, elementType);
            }
        }

        private void FixList(IList list, Type elementType)
        {
            if (list.Count >= MinCount) return;
            if (elementType == null) return;

            var canConstruct = elementType.IsValueType
                               || elementType.GetConstructor(Type.EmptyTypes) != null;
            if (!canConstruct) return;

            while (list.Count < MinCount)
                list.Add(Activator.CreateInstance(elementType));
        }
    }
}
