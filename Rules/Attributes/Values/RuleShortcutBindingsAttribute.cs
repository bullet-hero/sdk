using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BH.SDK.Utils;

namespace BH.SDK.Rules.Attributes
{
    // Checks SHAPE only (plausible id, legal binding in canonical form); the catalog and the key table
    // live in consumers. Fix DROPS an offending entry, so the shortcut falls back to its default.
    // Why canonical and why modifier-only is allowed: BH.SDK/Docs/RULES.md, "RuleShortcutBindings".

    /// <summary>
    /// A keybindings map must hold canonical binding strings under non-empty ids. Fix removes every
    /// entry that is not, so the shortcut falls back to its shipped default.
    /// </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleShortcutBindingsAttribute : BasePropertyRuleAttribute
    {
        /// <summary> <c>"rule_shortcut_bindings"</c>, the key its message is looked up under. </summary>
        public override string RuleNameKey => "rule_shortcut_bindings";

        // Warning, not Error, and the Fix is why: dropping a malformed entry hands the shortcut back to
        // its shipped default, so nothing stops working. It is a settings file that lost one
        // rebinding, not a file that cannot be used.

        /// <summary> A warning: the level still plays, but this is not what the author meant. </summary>
        public override RuleGroup Group => RuleGroup.Warning;

        /// <summary> Applies to the keybinding override map. </summary>
        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(IDictionary).IsAssignableFrom(property.PropertyType);

        /// <summary> Passes when every value is in canonical shortcut form. </summary>
        protected override bool IsValidInternal(object value, RuleContext context)
        {
            if (value is not IDictionary dictionary) return false;

            foreach (DictionaryEntry entry in dictionary)
                if (!IsValidEntry(entry.Key, entry.Value)) return false;
            return true;
        }

        /// <summary> Drops the entries that are not, so the shipped default takes over for those. </summary>
        protected override void FixInternal(object target, PropertyInfo property, RuleContext context)
        {
            if (property.GetValue(target) is not IDictionary dictionary) return;

            var doomed = new List<object>();
            foreach (DictionaryEntry entry in dictionary)
                if (!IsValidEntry(entry.Key, entry.Value)) doomed.Add(entry.Key);

            foreach (var key in doomed) dictionary.Remove(key);
        }

        private static bool IsValidEntry(object key, object value)
        {
            if (key is not string shortcutId) return false;
            if (shortcutId.Length == 0) return false;
            if (shortcutId.Length > KeybindingsRules.MaxShortcutIdLength) return false;

            if (value is not string binding) return false;
            if (!ShortcutSyntax.TryNormalize(binding, allowModifierOnly: true, out var normalized)) return false;

            return binding == normalized;
        }
    }
}
