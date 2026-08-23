using System;
using System.Collections.Generic;
using System.Text;
using BH.SDK.Models.Enums.Controls;
using BH.SDK.Rules;

namespace BH.SDK.Utils
{
    // The whole grammar of a keybinding, and the only place it exists. A binding is stored as a
    // STRING rather than as a packed struct because it is the one form every consumer of the file
    // already reads: a person hand-editing settings.json, a diff, a bug report screenshot.
    //
    // What this file deliberately does NOT know is which keys a keyboard has. A full key enum here
    // would be exactly what KeyBindingMask's own header argues against - a second source of truth
    // against the engine's - and it would need maintaining against every key Unity adds. So the SDK
    // validates SHAPE (lowercase, canonical modifier order, alternate count, length) and the engine
    // side resolves the key name against UnityEngine.InputSystem.Key. A name nothing recognizes is
    // shape-valid here and resolves to the catalog default there, with one warning.
    //
    // That split is also why "mouse.middle" and "gamepad.south" parse: reserving the token shape now
    // means adding pointer or pad bindings later is a consumer change, not a format change.

    /// <summary>
    /// Parses and canonicalizes a keybinding string: <c>alternate ("|" alternate)*</c>, where an
    /// alternate is <c>(modifier "+")* key</c> and an empty string means "not bound".
    /// </summary>
    public static class ShortcutSyntax
    {
        public const char AlternateSeparator = '|';
        public const char ModifierSeparator = '+';

        public const string Ctrl = "ctrl";
        public const string Shift = "shift";
        public const string Alt = "alt";

        /// <summary> The value meaning "this shortcut answers to nothing". </summary>
        public const string Unbound = "";

        // Canonical order is fixed rather than "as authored" so that equality is string equality:
        // "Shift+Ctrl+P" and "ctrl+shift+p" are the same binding, and the map must not be able to
        // hold both. Everything downstream - conflict detection, the settings row's own display -
        // then compares strings instead of parsing twice.

        /// <summary>
        /// Rewrites <paramref name="value"/> into its one canonical form. Returns false when the
        /// value is not a legal binding at all, in which case <paramref name="normalized"/> is
        /// <see cref="Unbound"/>. <paramref name="allowModifierOnly"/> permits an alternate that is
        /// nothing but modifiers, which only a held shortcut (a wheel modifier) wants.
        /// </summary>
        public static bool TryNormalize(string value, bool allowModifierOnly, out string normalized)
        {
            normalized = Unbound;
            if (value == null) return false;
            if (value.Length > KeybindingsRules.MaxBindingLength) return false;

            var trimmed = value.Trim();
            if (trimmed.Length == 0) return true; // explicitly unbound, and that is a legal value

            var parts = trimmed.Split(AlternateSeparator);
            if (parts.Length > KeybindingsRules.MaxAlternates) return false;

            var accepted = new List<string>(parts.Length);
            foreach (var part in parts)
            {
                if (!TryNormalizeAlternate(part, allowModifierOnly, out var alternate)) return false;

                // An empty alternate inside a value ("ctrl+c|") is a typo rather than a second,
                // unbound key - there is no such thing. Dropping it is the repair.
                if (alternate.Length == 0) continue;
                if (accepted.Contains(alternate)) continue;

                accepted.Add(alternate);
            }

            normalized = string.Join(AlternateSeparator.ToString(), accepted);
            return true;
        }

        /// <summary> One alternate, canonicalized. Empty in, empty out. </summary>
        public static bool TryNormalizeAlternate(string alternate, bool allowModifierOnly,
            out string normalized)
        {
            normalized = Unbound;
            if (alternate == null) return false;

            if (!TrySplit(alternate, out var modifiers, out var key)) return false;
            if (key.Length == 0)
            {
                if (modifiers == ShortcutModifiers.None) return true; // an empty alternate
                if (!allowModifierOnly) return false;
            }

            normalized = Compose(modifiers, key);
            return true;
        }

        // The one method every consumer outside the SDK actually calls: it hands back the modifier
        // set and the raw key NAME, which is where the engine takes over. Nothing here maps that
        // name to anything.

        /// <summary>
        /// Splits one alternate into its modifiers and its key name. The key is empty for a
        /// modifiers-only alternate and for an empty input.
        /// </summary>
        public static bool TrySplit(string alternate, out ShortcutModifiers modifiers, out string key)
        {
            modifiers = ShortcutModifiers.None;
            key = string.Empty;

            if (alternate == null) return false;

            var trimmed = alternate.Trim();
            if (trimmed.Length == 0) return true;

            var tokens = trimmed.Split(ModifierSeparator);
            for (var i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i].Trim().ToLowerInvariant();
                if (token.Length == 0) return false; // "ctrl+", "+c", "ctrl++c"

                var modifier = ToModifier(token);
                if (modifier != ShortcutModifiers.None)
                {
                    // A modifier in the last slot is the key half only when it is the ONLY token -
                    // "ctrl+shift" is a typo, "ctrl" is a legal held binding.
                    if (i == tokens.Length - 1 && tokens.Length > 1) return false;

                    modifiers |= modifier;
                    continue;
                }

                if (i != tokens.Length - 1) return false; // a key before a modifier
                if (!IsKeyName(token)) return false;

                key = token;
            }

            return true;
        }

        /// <summary> The canonical string for a modifier set plus a key name. </summary>
        public static string Compose(ShortcutModifiers modifiers, string key)
        {
            var builder = new StringBuilder(KeybindingsRules.MaxBindingLength);

            if ((modifiers & ShortcutModifiers.Ctrl) != 0) Append(builder, Ctrl);
            if ((modifiers & ShortcutModifiers.Shift) != 0) Append(builder, Shift);
            if ((modifiers & ShortcutModifiers.Alt) != 0) Append(builder, Alt);
            if (!string.IsNullOrEmpty(key)) Append(builder, key);

            return builder.ToString();
        }

        /// <summary> The alternates of an already-normalized value, in order. </summary>
        public static string[] SplitAlternates(string normalized)
        {
            return string.IsNullOrEmpty(normalized)
                ? Array.Empty<string>()
                : normalized.Split(AlternateSeparator);
        }

        /// <summary> Whether a token names one of the three modifiers. </summary>
        public static bool IsModifier(string token) => ToModifier(token) != ShortcutModifiers.None;

        private static ShortcutModifiers ToModifier(string token) => token switch
        {
            Ctrl => ShortcutModifiers.Ctrl,
            Shift => ShortcutModifiers.Shift,
            Alt => ShortcutModifiers.Alt,
            _ => ShortcutModifiers.None,
        };

        // Lowercase letters, digits, underscore and a dot, the last one reserving the "mouse.middle"
        // shape. No hyphen: it reads as a minus sign in a value that already uses '+' as a joiner.
        private static bool IsKeyName(string token)
        {
            foreach (var symbol in token)
            {
                var legal = symbol is >= 'a' and <= 'z'
                            || symbol is >= '0' and <= '9'
                            || symbol == '_' || symbol == '.';
                if (!legal) return false;
            }

            return token.Length > 0;
        }

        private static void Append(StringBuilder builder, string token)
        {
            if (builder.Length > 0) builder.Append(ModifierSeparator);
            builder.Append(token);
        }
    }
}