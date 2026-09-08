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

namespace BH.SDK.Services
{
    // Builds LevelHints.FontCharacters - the per-font distinct-character sets a player warms a
    // glyph atlas from. This lives in the SDK rather than in a host's editor so a third-party tool
    // writes byte-identical sets: the hint is only worth trusting blindly (which every reader does)
    // if everyone who writes one agrees on what it should contain.
    //
    // Building is split from writing on purpose. Build/BuildAll are pure and return values, so a
    // caller that must journal its writes - the gen_font_cache generator, whose undo depends on it -
    // can route them through its own mechanism, while a caller that owns the model outright uses
    // Apply. Merging the two would force the generator to either duplicate the algorithm or break
    // undo.
    //
    // Localized text makes the set localized too: one language's alphabet is not another's, so
    // warming the union everywhere would drag a Cyrillic level's glyphs into an English player's
    // atlas. Text that is NOT localized is visible whatever the language, so its characters land in
    // every language's set rather than in one of them.

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

        // SortedSet everywhere rather than HashSet: the output is written to a file that the author
        // diffs and version-controls, so re-saving an unchanged level must produce an unchanged
        // line. Hash iteration order is not contractually stable across runtimes, so it would not.

        /// <summary> One font's accumulating character sets - a shared one for text that reads the
        /// same in every language, plus one per language that doesn't. </summary>
        private sealed class CharacterSets
        {
            private readonly SortedSet<char> _shared = new();
            private readonly SortedDictionary<string, SortedSet<char>> _perLanguage =
                new(System.StringComparer.Ordinal);

            // The appearing mask goes into the SHARED set, not a language's: a mask character is
            // drawn in place of whatever the text says, so it is needed in every language the object
            // can be read in. Missing it is invisible until the effect actually runs, at which point
            // the hidden characters render as boxes - the one moment the author is looking straight
            // at them.

            /// <summary> Folds one text object's characters into the set. </summary>
            public void Add(TextObject textObject)
            {
                Add(textObject.Text);
                AddChars(_shared, textObject.AppearingMask);
            }

            private void Add(IString text)
            {
                switch (text)
                {
                    case StringValue value:
                        AddChars(_shared, value.Value);
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
                                set = new SortedSet<char>();
                                _perLanguage.Add(code, set);
                            }
                            AddChars(set, entry.Value);
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

            private static void AddChars(ISet<char> set, string text)
            {
                if (string.IsNullOrEmpty(text)) return;
                foreach (var character in text)
                    set.Add(character);
            }

            // Truncation is silent and deliberate: the set is advisory, so a CJK level that overruns
            // the cap warms a prefix and renders the rest on demand, which is strictly better than
            // refusing to build a set at all. Merging happens before the cap so a shared character
            // can't be dropped in favour of a language-specific one that sorts earlier.
            private static string Join(SortedSet<char> own, SortedSet<char> shared)
            {
                var merged = shared == null ? own : new SortedSet<char>(own);
                if (shared != null) merged.UnionWith(shared);

                var builder = new StringBuilder(merged.Count);
                foreach (var character in merged)
                {
                    if (builder.Length == TextRules.MaxFontBufferSize) break;
                    builder.Append(character);
                }
                return builder.ToString();
            }
        }
    }
}
