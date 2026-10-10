using System.Collections.Generic;
using System.Text;
using BH.SDK.Models.Data;
using BH.SDK.Models.Hints;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Utils;

namespace BH.SDK.Services
{
    // Builds LevelHints.FontCharacters. In the SDK so a third-party tool writes byte-identical sets.
    // Build/BuildAll are pure (a journaling caller routes them itself); Apply writes. Localized text
    // makes the set localized, non-localized text lands in every language's set. Docs/ARCHIVE.md.

    /// <summary>
    /// The one algorithm for turning a scope's text objects into per-font character sets.
    /// </summary>
    public static class FontCharacterService
    {
        /// <summary> Character sets for every font the scope's text objects actually reference, in
        /// the shape LevelHints.FontCharacters expects. Fonts with no text are absent rather
        /// than empty - an absent entry and an empty one mean the same thing to a reader, and
        /// leaving them out keeps the file smaller. </summary>
        public static Dictionary<FontResourceId, CachedFontText> BuildAll(IObjectScope scope)
        {
            var built = new Dictionary<FontResourceId, CachedFontText>();
            if (scope?.Objects == null) return built;

            var collected = Collect(scope);
            foreach (var (fontResourceId, sets) in collected)
            {
                var characters = sets.ToValue();
                if (characters != null) built.Add(fontResourceId, new CachedFontText(fontResourceId, characters));
            }

            return built;
        }

        /// <summary> The character set for one font, or null when no text object in the scope uses
        /// it. </summary>
        public static CachedFontText Build(IObjectScope scope, FontResourceId fontResourceId)
        {
            if (scope?.Objects == null) return null;

            var sets = new CharacterSets();
            foreach (var obj in scope.Objects.Values)
            {
                if (obj is not TextObject textObject) continue;
                if (!textObject.FontResourceId.Equals(fontResourceId)) continue;
                sets.Add(textObject);
            }

            var characters = sets.ToValue();
            return characters == null ? null : new CachedFontText(fontResourceId, characters);
        }

        /// <summary> Overwrites <paramref name="hints"/>' sets with freshly built ones. With
        /// <paramref name="removeUnused"/> the result is exactly what the scope contains now, which
        /// is what "rebuild from scratch" means; without it, entries for fonts no longer referenced
        /// survive untouched. Writes the dictionary directly, so it must not be used from a
        /// generator - see the note at the top of this file. </summary>
        public static void Apply(LevelHints hints, IObjectScope scope, bool removeUnused = true)
        {
            if (hints?.FontCharacters == null) return;

            var built = BuildAll(scope);
            if (removeUnused) hints.FontCharacters.Clear();

            foreach (var (fontResourceId, cached) in built)
                hints.FontCharacters[fontResourceId] = cached;
        }

        /// <summary> Every font id the scope's text objects reference, so a caller can tell which
        /// existing entries have gone stale. </summary>
        public static HashSet<FontResourceId> CollectFontIds(IObjectScope scope)
        {
            var ids = new HashSet<FontResourceId>();
            if (scope?.Objects == null) return ids;

            foreach (var obj in scope.Objects.Values)
                if (obj is TextObject textObject)
                    ids.Add(textObject.FontResourceId);
            return ids;
        }

        private static Dictionary<FontResourceId, CharacterSets> Collect(IObjectScope scope)
        {
            var collected = new Dictionary<FontResourceId, CharacterSets>();
            foreach (var obj in scope.Objects.Values)
            {
                if (obj is not TextObject textObject) continue;

                if (!collected.TryGetValue(textObject.FontResourceId, out var sets))
                {
                    sets = new CharacterSets();
                    collected.Add(textObject.FontResourceId, sets);
                }

                sets.Add(textObject);
            }

            return collected;
        }

        // SortedSet, not HashSet (the output is diffed; hash order is not stable across runtimes). The
        // sets hold CODE POINTS: per code unit an astral character would write unpaired surrogates.

        /// <summary> One font's accumulating character sets - a shared one for text that reads the
        /// same in every language, plus one per language that doesn't. </summary>
        private sealed class CharacterSets
        {
            private readonly SortedSet<int> _shared = new();

            private readonly SortedDictionary<string, SortedSet<int>> _perLanguage =
                new(System.StringComparer.Ordinal);

            // The appearing mask goes into the SHARED set: it is drawn in place of the text in every
            // language, and a miss shows as boxes exactly when the author is looking.

            /// <summary> Folds one text object's characters into the set. </summary>
            public void Add(TextObject textObject)
            {
                Add(textObject.Text);
                AddCharacters(_shared, textObject.AppearingMask);
            }

            private void Add(IString text)
            {
                switch (text)
                {
                    case StringValue value:
                        AddCharacters(_shared, value.Value);
                        break;

                    case StringLocalized localized:
                    {
                        if (localized.Strings == null) break;
                        foreach (var entry in localized.Strings)
                        {
                            if (entry == null) continue;
                            var code = string.IsNullOrEmpty(entry.LanguageCode)
                                ? ValueRules.DefaultLanguageCode
                                : entry.LanguageCode;

                            if (!_perLanguage.TryGetValue(code, out var set))
                            {
                                set = new SortedSet<int>();
                                _perLanguage.Add(code, set);
                            }

                            AddCharacters(set, entry.Value);
                        }

                        break;
                    }
                }
            }

            /// <summary> The set as the string a level stores - sorted, so a re-save does not churn the file. </summary>
            public IString ToValue()
            {
                if (_perLanguage.Count == 0)
                    return _shared.Count == 0 ? null : new StringValue(Join(_shared, null));

                var strings = new List<StringLanguage>(_perLanguage.Count);
                foreach (var (code, set) in _perLanguage)
                    strings.Add(new StringLanguage(code, Join(set, _shared)));
                return new StringLocalized(strings);
            }

            // A surrogate half with no partner is stored as itself rather than repaired or dropped:
            // it is authored damage this service did not cause, and a census that silently mends its
            // input stops being a census. Join writes such an entry back out unchanged.
            private static void AddCharacters(ISet<int> set, string text)
            {
                if (string.IsNullOrEmpty(text)) return;

                for (var index = 0; index < text.Length; index++)
                {
                    var character = text[index];
                    if (SurrogateUtils.IsLead(character) && index + 1 < text.Length
                                                         && SurrogateUtils.IsTrail(text[index + 1]))
                    {
                        set.Add(char.ConvertToUtf32(character, text[index + 1]));
                        index++;
                        continue;
                    }

                    set.Add(character);
                }
            }

            // Truncation is silent: the set is advisory. Merge BEFORE the cap; the cap counts code units
            // but a character is taken whole or not at all.
            private static string Join(SortedSet<int> own, SortedSet<int> shared)
            {
                var merged = shared == null ? own : new SortedSet<int>(own);
                if (shared != null) merged.UnionWith(shared);

                var builder = new StringBuilder(merged.Count);
                foreach (var codePoint in merged)
                {
                    var width = codePoint > char.MaxValue ? 2 : 1;
                    if (builder.Length + width > TextRules.MaxFontBufferSize) break;

                    if (width == 1) builder.Append((char)codePoint);
                    else builder.Append(char.ConvertFromUtf32(codePoint));
                }

                return builder.ToString();
            }
        }
    }
}
