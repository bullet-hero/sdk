using System;
using System.Collections.Generic;
using BH.SDK.Models.Data;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using BH.SDK.Versions;
using Newtonsoft.Json;

// ReSharper disable NonReadonlyMemberInGetHashCode

namespace BH.SDK.Models.Hints
{
    // The level's fifth aggregate, and the only one that carries nothing authored. Everything here
    // is DERIVED from Settings/Game/Audio/Resources, written by whoever saves the level, and safe to
    // throw away: a consumer that ignores this whole object plays the exact same level, only paying
    // at load or mid-playback for work a hint would have front-loaded. That is the membership test -
    // a field belongs here when it can be recomputed from the rest of the level and nothing looks
    // different when it is missing, wrong or stale.
    //
    // It exists as its own aggregate rather than as fields on the aggregates the hints describe
    // (Limits used to sit on LevelSettings, FontCharacters on LevelResources) because those two
    // neighbourhoods are authoritative: a reader there cannot tell "the author decided this" from
    // "a tool measured this last save". Everything advisory living behind one property makes the
    // distinction structural - and makes "recompute every hint" and "drop every hint" one call each
    // rather than a list someone has to keep current.
    //
    // Consequence for the format: a level written before this existed deserializes to an all-empty
    // Hints, which is exactly "no hint", so the move needed no migration and the Level domain stays
    // at (1, 0). A level whose hints were written by an older layout simply loses them at the next
    // save, which is what advisory means.

    /// <summary>
    /// Everything measured ABOUT the level rather than authored in it: preallocation sizes, warm-up
    /// sets. Strictly advisory - never authoritative, never required, never trusted blindly.
    /// </summary>
    [RuleContainer]
    [DataVersion(DataDomains.LevelHints, 1, 0)]
    public class LevelHints : IModel<LevelHints>
    {
        /// <summary> Peak simultaneous objects per type, refreshed on every editor save - see
        /// <see cref="LimitHints"/> and <see cref="LevelCapacityUtils.GetPeakUsage"/>. All zeroes
        /// means "not measured", not "empty level". </summary>
        [RuleNotNull]
        [JsonProperty(Names.Limits)]
        public LimitHints Limits { get; set; }

        /// <summary> Per-font distinct-character sets, an advisory glyph-atlas warm-up hint. </summary>
        [RuleNotNull, RuleCollectionMaxCount(ResourceRules.MaxFontCharacterEntries)]
        [RuleDictionaryKeyMatches(nameof(CachedFontText.FontResourceId))]
        [JsonProperty(Names.FontCharacters)]
        public Dictionary<FontResourceId, CachedFontText> FontCharacters { get; set; }

        /// <summary> Was anything ever measured into this level at all? </summary>
        [JsonIgnore]
        public bool HasValue => (Limits?.HasValue ?? false) || FontCharacters is { Count: > 0 };

        public LevelHints()
        {
            Limits = new LimitHints();
            FontCharacters = new Dictionary<FontResourceId, CachedFontText>();
        }
        public LevelHints(LimitHints limits, Dictionary<FontResourceId, CachedFontText> fontCharacters)
        {
            Limits = limits;
            FontCharacters = fontCharacters;
        }

        public void Reset()
        {
            Limits.Reset();
            FontCharacters.Clear();
        }

        public object Clone() => Copy();
        public LevelHints Copy() => new(Limits?.Copy(), FontCharacters.CopyDictionary());

        public void Update(LevelHints src)
        {
            Limits = src.Limits?.Copy();
            FontCharacters = src.FontCharacters.CopyDictionary();
        }

        public void Pull(LevelHints src)
        {
            Limits = Limits.PullFrom(src.Limits);
            FontCharacters = src.FontCharacters.CopyDictionary();
        }

        public override bool Equals(object obj) => obj is LevelHints value && Equals(value);
        public override int GetHashCode() => HashCode.Combine(Limits, FontCharacters.GetDictionaryHashCode());

        public bool Equals(LevelHints other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            var result = Equals(Limits, other.Limits)
                         && FontCharacters.DictionaryEquals(other.FontCharacters);
            return result;
        }
    }
}
