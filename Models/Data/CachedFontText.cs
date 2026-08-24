using System;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Interfaces.Values;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Data
{
    // Carries its own FontResourceId so LevelHints.FontCharacters can serialize the way every
    // resource dictionary in the format does - as a plain array with the key dropped and recovered
    // from the value on read (DictionaryAsListConverter). The alternative, a bare id -> IString map,
    // needs the {k, v} pair form instead, which reads differently from every other keyed collection
    // for no reason a file's reader could guess.

    /// <summary>
    /// One font's distinct-character set: every character the level's text draws in that typeface,
    /// as an advisory glyph-atlas warm-up hint.
    /// </summary>
    [RuleContainer]
    public class CachedFontText : IModel<CachedFontText>
    {
        /// <summary> Which typeface this set belongs to. Unlike the other resource dictionaries this
        /// spans the whole id range, game-defined ids included - the bundled font needs warming as
        /// much as a shipped one. </summary>
        [RuleIPrimitiveIntNotNull]
        [JsonProperty(Names.FontResourceId)]
        public FontResourceId FontResourceId { get; set; }

        // Localized, because one language's alphabet is not another's: warming the union everywhere
        // would drag a Cyrillic level's glyphs into an English player's atlas. Being an IString also
        // means it resolves through exactly the same path the text it describes does.

        /// <summary> The characters themselves, in a stable order, per language where the level's
        /// text is localized. </summary>
        [RuleNotNull(typeof(StringValue)), RuleIStringMax(TextRules.MaxFontBufferSize)]
        [JsonProperty(Names.Chars)]
        public IString Characters { get; set; }

        public CachedFontText()
        {
            FontResourceId = FontResourceId.Default;
            Characters = new StringValue();
        }
        public CachedFontText(FontResourceId fontResourceId, IString characters)
        {
            FontResourceId = fontResourceId;
            Characters = characters;
        }
        public void Reset()
        {
            FontResourceId = FontResourceId.Default;
            Characters = new StringValue();
        }

        public object Clone() => Copy();
        public CachedFontText Copy() => new(FontResourceId, Characters.Copy());

        public void Update(CachedFontText src)
        {
            FontResourceId = src.FontResourceId;
            Characters = src.Characters.Copy();
        }

        public void Pull(CachedFontText src)
        {
            FontResourceId = src.FontResourceId;
            Characters = Characters.PullFrom(src.Characters);
        }

        public override bool Equals(object obj) => obj is CachedFontText value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(FontResourceId, Characters);

        public bool Equals(CachedFontText other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = FontResourceId.Equals(other.FontResourceId)
                         && Characters.Equals(other.Characters);
            return result;
        }
    }
}
