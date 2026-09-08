using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace BH.SDK.Rules.Attributes
{
    // Every id-keyed dictionary in the format stores the id twice: once as the key, once inside the
    // value. Serialization hides the redundancy - DictionaryAsListConverter drops the key on write
    // and rebuilds it from the value on read, so a round trip can never disagree - but code can. An
    // editor that mutates obj.ObjectId without re-keying leaves a dictionary where lookup by id
    // finds nothing while iteration finds the object, and the two halves of the program disagree
    // about what exists.
    //
    // The rule sits on the dictionary property rather than on the key, because a key is not a
    // property and cannot be addressed by a RulePath - see the note in RuleAnalyzer.

    /// <summary> Each dictionary key must equal its value's own id property. </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleDictionaryKeyMatchesAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_dictionary_key_matches"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_dictionary_key_matches";

        /// <summary> Which property of a value must equal the key it is filed under. </summary>
        public string ValuePropertyName { get; set; }

        /// <summary> Takes which property of a value must equal its key. </summary>
        public RuleDictionaryKeyMatchesAttribute(string valuePropertyName)
        {
            ValuePropertyName = valuePropertyName;
        }

        /// <summary> Applies to any dictionary. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(IDictionary).IsAssignableFrom(property.PropertyType);

        /// <summary> Passes when every key equals the id its own value carries. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
        {
            if (value is not IDictionary dictionary) return false;

            PropertyInfo idProperty = null;

            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Value == null) continue;

                idProperty ??= entry.Value.GetType().GetProperty(ValuePropertyName);
                if (idProperty == null) return false;

                if (!Equals(entry.Key, idProperty.GetValue(entry.Value))) return false;
            }
            return true;
        }

        // Re-key rather than rewrite the value: the value's own id is the authored intent, the key
        // is bookkeeping. Rebuilding the dictionary is the only way to do it - a key cannot be
        // changed in place - and entries whose ids collide after re-keying collapse into one, which
        // is the honest outcome of two objects claiming the same identity.

        /// <summary> Re-files each entry under the id its value carries, which is the half that cannot be wrong. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            if (property.GetValue(target) is not IDictionary dictionary) return;

            var entries = new List<DictionaryEntry>(dictionary.Count);
            foreach (DictionaryEntry entry in dictionary) entries.Add(entry);

            PropertyInfo idProperty = null;
            foreach (var entry in entries)
            {
                if (entry.Value == null) continue;

                idProperty = entry.Value.GetType().GetProperty(ValuePropertyName);
                if (idProperty != null) break;
            }
            if (idProperty == null) return;

            dictionary.Clear();
            foreach (var entry in entries)
            {
                if (entry.Value == null) continue;

                var id = idProperty.GetValue(entry.Value);
                if (id == null) continue;

                dictionary[id] = entry.Value;
            }
        }
    }
}
