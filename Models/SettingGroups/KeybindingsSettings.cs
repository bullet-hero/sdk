using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups
{
    // A SPARSE map of what a player moved: absent takes the catalog default, reset is a removal. A string
    // so settings.json stays readable (ShortcutSyntax owns it). Docs/SETTING_GROUPS.md

    /// <summary>
    /// The player's own keyboard shortcuts, as overrides on top of the game's shipped defaults:
    /// shortcut id to binding string, holding only what differs.
    /// </summary>
    [RuleContainer]
    [GenerateModel]
    public sealed partial class KeybindingsSettings : IModel<KeybindingsSettings>, IMoveable<KeybindingsSettings>
    {
        /// <summary> Shortcut id to its binding, where an empty value means the player unbound it -
        /// which is not the same as being absent, and must not fall back to the default. </summary>
        [RuleNotNull, RuleCollectionMaxCount(KeybindingsRules.MaxOverrides), RuleShortcutBindings]
        [JsonProperty(Names.Keys)]
        public Dictionary<string, string> Overrides { get; set; }

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public KeybindingsSettings()
        {
            Overrides = new Dictionary<string, string>();
        }

        /// <summary> Built from its dictionary and overrides. </summary>
        public KeybindingsSettings(Dictionary<string, string> overrides)
        {
            Overrides = overrides;
        }

        // An unbound entry is the empty string, so "overridden" is not ContainsKey's question.

        /// <summary> The player's binding for <paramref name="shortcutId"/>, or null when they never
        /// set one. An empty string is a real answer: they cleared it. </summary>
        public string GetOverride(string shortcutId)
            => shortcutId != null && Overrides.TryGetValue(shortcutId, out var binding) ? binding : null;

        /// <summary> Records a binding, canonicalizing it first. Returns false when the value is not
        /// a legal binding, in which case nothing is written. </summary>
        public bool SetOverride(string shortcutId, string binding, bool allowModifierOnly = false)
        {
            if (string.IsNullOrEmpty(shortcutId)) return false;
            if (shortcutId.Length > KeybindingsRules.MaxShortcutIdLength) return false;
            if (!ShortcutSyntax.TryNormalize(binding, allowModifierOnly, out var normalized)) return false;

            Overrides[shortcutId] = normalized;
            return true;
        }

        /// <summary> Drops one override, so the id falls back to its shipped default. </summary>
        public bool ClearOverride(string shortcutId)
            => shortcutId != null && Overrides.Remove(shortcutId);

        // A shallow dictionary copy IS a deep one here - both halves are strings, which are
        // immutable. ModelUtils' three CopyDictionary overloads all refuse this pair anyway: string
        // is neither unmanaged nor ICopyable<string>.
    }
}