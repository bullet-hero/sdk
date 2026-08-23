using System.Collections.Generic;
using BH.SDK.Models.Interfaces;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

namespace BH.SDK.Models.SettingGroups
{
    // A SPARSE map, and that is the whole design. The shortcuts a game has, what each is called and
    // what key it ships on are code - they change with the code, and a settings file that pinned all
    // of them would go stale the moment a shortcut is added or its default is improved. So this
    // holds only the entries a player actually moved: an id absent here takes the catalog's default,
    // and a "reset to default" is a removal rather than a write.
    //
    // The value is a STRING rather than a packed number for the same reason a level's own format is
    // readable: settings.json is a file people open. "ctrl+shift+d" survives a diff, a bug report and
    // a hand edit; a bitfield survives none of them. ShortcutSyntax owns what the string may say.
    //
    // Nothing here knows which ids exist or which key names a keyboard has - both are the consumer's
    // (see the header on ShortcutSyntax). An unknown id and an unrecognized key name are equally
    // harmless: neither resolves, so neither takes a shortcut away.

    /// <summary>
    /// The player's own keyboard shortcuts, as overrides on top of the game's shipped defaults:
    /// shortcut id to binding string, holding only what differs.
    /// </summary>
    [RuleContainer]
    public class KeybindingsSettings : IModel<KeybindingsSettings>, IMoveable<KeybindingsSettings>
    {
        /// <summary> Shortcut id to its binding, where an empty value means the player unbound it -
        /// which is not the same as being absent, and must not fall back to the default. </summary>
        [RuleNotNull, RuleCollectionMaxCount(KeybindingsRules.MaxOverrides), RuleShortcutBindings]
        [JsonProperty(Names.Keys)]
        public Dictionary<string, string> Overrides { get; set; }

        public KeybindingsSettings()
        {
            Overrides = new Dictionary<string, string>();
        }

        public KeybindingsSettings(Dictionary<string, string> overrides)
        {
            Overrides = overrides;
        }

        public void Reset()
        {
            Overrides.Clear();
        }

        // Whether an id is overridden at all is the question every consumer asks, and it is not
        // ContainsKey: an unbound entry is stored as the empty string, so a caller reading the value
        // straight out of the dictionary must not confuse "the player cleared this" with "the player
        // never touched it".

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

        public object Clone() => Copy();

        // A shallow dictionary copy IS a deep one here - both halves are strings, which are
        // immutable. ModelUtils' three CopyDictionary overloads all refuse this pair anyway: string
        // is neither unmanaged nor ICopyable<string>.
        public KeybindingsSettings Copy() => new(new Dictionary<string, string>(Overrides));

        public void Pull(KeybindingsSettings source)
        {
            Overrides = new Dictionary<string, string>(source.Overrides);
        }

        public override bool Equals(object obj) => obj is KeybindingsSettings value && Equals(value);
        public override int GetHashCode() => Overrides.GetDictionaryHashCode();

        public bool Equals(KeybindingsSettings other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Overrides.DictionaryEquals(other.Overrides);
        }
    }
}