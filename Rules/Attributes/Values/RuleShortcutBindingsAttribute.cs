using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BH.SDK.Utils;

namespace BH.SDK.Rules.Attributes
{
    // What this can and cannot check is the same split the whole keybindings design rests on. It
    // checks SHAPE - every key is a plausible shortcut id, every value is a legal binding in its one
    // canonical form - and it deliberately checks neither that the id names a shortcut the game has
    // nor that the key name is one a keyboard reports. Both of those live in the consumer's catalog
    // and in the engine's key table, neither of which the SDK may see (see ShortcutSyntax's header).
    //
    // Canonical form rather than merely parseable form, because equality downstream is string
    // equality: a file carrying "Shift+Ctrl+P" would resolve correctly and still fail to match the
    // "ctrl+shift+p" a conflict finder compares it against. Reporting it is what keeps the two from
    // ever disagreeing.
    //
    // Fix DROPS an offending entry rather than repairing it, and that is the safe direction: a
    // dropped override falls back to the shipped default, so the shortcut keeps working. Rewriting a
    // garbled value into some nearby legal one would hand the player a binding they never chose.
    //
    // allowModifierOnly is true here on purpose. The rule cannot tell a held shortcut from a pressed
    // one - only the catalog knows - so it accepts the wider shape and lets the resolver reject a
    // modifiers-only binding on a shortcut that cannot use one.

    /// <summary>
    /// A keybindings map must hold canonical binding strings under non-empty ids. Fix removes every
    /// entry that is not, so the shortcut falls back to its shipped default.
    /// </summary>
    [AttributeUsage(PropertyTarget)]
    public class RuleShortcutBindingsAttribute : BasePropertyRuleAttribute
    {
        public override string RuleNameKey => "rule_shortcut_bindings";

        protected override bool IsValidTypeInternal(PropertyInfo property)
            => typeof(IDictionary).IsAssignableFrom(property.PropertyType);

        protected override bool IsValidInternal(object value, RuleContext context)
        {
            if (value is not IDictionary dictionary) return false;

            foreach (DictionaryEntry entry in dictionary)
                if (!IsValidEntry(entry.Key, entry.Value)) return false;
            return true;
        }

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
