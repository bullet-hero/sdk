using System;
using System.Collections.Generic;
using BH.SDK.Models.Attributes;
using BH.SDK.Models.Data;
using BH.SDK.Models.Interfaces;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Rules;
using BH.SDK.Rules.Attributes;
using BH.SDK.Utils;
using BH.SDK.Versions;
using Newtonsoft.Json;

namespace BH.SDK.Models.Hints
{
    // The only aggregate carrying nothing authored: everything is DERIVED, safe to throw away, and a
    // missing, wrong or stale field changes nothing visible. Kept apart from the aggregates it
    // describes so "authored" vs "measured" is structural. Models/Hints/CLAUDE.md

    /// <summary>
    /// Everything measured ABOUT the level rather than authored in it: preallocation sizes, warm-up
    /// sets. Strictly advisory - never authoritative, never required, never trusted blindly.
    /// </summary>
    [RuleContainer]
    [ModelGeneration(ModelDomains.LevelHints, ModelGenerations.V1_AlphaRelease)]
    [GenerateModel]
    public sealed partial class LevelHints : IModel<LevelHints>
    {
        /// <summary> Peak simultaneous objects per type, refreshed on every editor save - see
        /// <see cref="LimitHints"/> and <see cref="LevelCapacityUtils.GetPeakUsage"/>. All zeroes
        /// means "not measured", not "empty level". </summary>
        [RuleNotNull]
        [JsonProperty(Names.Limits)]
        public LimitHints Limits { get; set; }

        /// <summary> Per-font distinct-character sets, an advisory glyph-atlas warm-up hint. </summary>
        [GenerateModelKeyed(nameof(CachedFontText.FontResourceId))]
        [RuleNotNull, RuleCollectionMaxCount(ResourceRules.MaxFontCharacterEntries)]
        [RuleDictionaryKeyMatches(nameof(CachedFontText.FontResourceId))]
        [JsonProperty(Names.FontCharacters)]
        public Dictionary<FontResourceId, CachedFontText> FontCharacters { get; set; }

        /// <summary> Was anything ever measured into this level at all? </summary>
        [JsonIgnore]
        public bool HasValue => (Limits?.HasValue ?? false) || FontCharacters is { Count: > 0 };

        /// <summary> A fresh instance, every member at the value <c>Reset</c> restores. </summary>
        public LevelHints()
        {
            Limits = new LimitHints();
            FontCharacters = new Dictionary<FontResourceId, CachedFontText>();
        }
        /// <summary> Built from its limits, dictionary font resource id and characters. </summary>
        public LevelHints(LimitHints limits, Dictionary<FontResourceId, CachedFontText> fontCharacters)
        {
            Limits = limits;
            FontCharacters = fontCharacters;
        }
    }
}
