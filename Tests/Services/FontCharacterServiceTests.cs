using System.Linq;
using BH.SDK.Models;
using BH.SDK.Models.Data;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Rules;
using BH.SDK.Services;
using NUnit.Framework;

namespace BH.SDK.Tests.Services
{
    // The set this builds is trusted blindly by every reader, so what matters here is not that it is
    // "roughly right" but that it is exactly reproducible: same level in, same string out, byte for
    // byte, or a re-save churns the file and a foreign tool's set disagrees with the editor's.

    /// <summary> That the character set is exactly reproducible - same level in, same string out - or a re-save
    /// churns the file and a foreign tool's set disagrees with the editor's. </summary>
    public class FontCharacterServiceTests
    {
        private static readonly FontResourceId FontA = new(1);
        private static readonly FontResourceId FontB = new(-1);

        private static Level CreateLevel()
        {
            var level = new Level();
            level.Settings.Fps = 60;
            level.Settings.FrameDuration = 600;
            return level;
        }

        private static TextObject AddText(Level level, FontResourceId fontResourceId, string text)
        {
            var obj = new TextObject
            {
                ObjectId = level.Settings.GetNextObjectId(),
                Text = new StringValue(text),
                FontResourceId = fontResourceId,
                // Mask cleared so a test about TEXT is about text: TextObject's default mask is
                // "X", and it legitimately lands in every font's set (pinned by
                // Build_DefaultMaskIsIncluded), which would otherwise show up in every expectation
                // here as a stray leading character.
                AppearingMask = string.Empty,
            };
            level.Game.Objects.Add(obj.ObjectId, obj);
            return obj;
        }

        private static TextObject AddLocalizedText(Level level, FontResourceId fontResourceId,
            params StringLanguage[] strings)
        {
            var obj = new TextObject
            {
                ObjectId = level.Settings.GetNextObjectId(),
                Text = new StringLocalized(strings),
                FontResourceId = fontResourceId,
                // Mask cleared so a test about TEXT is about text: TextObject's default mask is
                // "X", and it legitimately lands in every font's set (pinned by
                // Build_DefaultMaskIsIncluded), which would otherwise show up in every expectation
                // here as a stray leading character.
                AppearingMask = string.Empty,
            };
            level.Game.Objects.Add(obj.ObjectId, obj);
            return obj;
        }

        private static string PlainOf(CachedFontText cached) => ((StringValue)cached.Characters).Value;

        private static string LanguageOf(CachedFontText cached, string code)
            => ((StringLocalized)cached.Characters).Strings.First(s => s.LanguageCode == code).Value;

        private static string BuildPlain(Level level, FontResourceId fontResourceId)
            => PlainOf(FontCharacterService.Build(level.Game, fontResourceId));

        // ==================== Plain text ====================

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Build_PlainTextOnly_ProducesStringValue()
        {
            var level = CreateLevel();
            AddText(level, FontA, "cab");

            var cached = FontCharacterService.Build(level.Game, FontA);

            Assert.IsInstanceOf<StringValue>(cached.Characters);
            Assert.AreEqual("abc", PlainOf(cached));
        }

        // The invariant DictionaryCachedFontTextsConverter rests on: the key is recovered from the
        // value on read, so a value carrying a different id than its key would silently relocate the
        // entry on the next round trip.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Build_StampsTheFontIdItWasAskedFor()
        {
            var level = CreateLevel();
            AddText(level, FontA, "abc");

            Assert.AreEqual(FontA, FontCharacterService.Build(level.Game, FontA).FontResourceId);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Build_RepeatedCharactersAcrossObjects_CollapseToOneEach()
        {
            var level = CreateLevel();
            AddText(level, FontA, "aab");
            AddText(level, FontA, "bbc");

            Assert.AreEqual("abc", BuildPlain(level, FontA));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Build_FontNoTextUses_ReturnsNull()
        {
            var level = CreateLevel();
            AddText(level, FontA, "abc");

            Assert.IsNull(FontCharacterService.Build(level.Game, FontB));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Build_OtherFontsText_DoesNotLeakIn()
        {
            var level = CreateLevel();
            AddText(level, FontA, "abc");
            AddText(level, FontB, "xyz");

            Assert.AreEqual("abc", BuildPlain(level, FontA));
            Assert.AreEqual("xyz", BuildPlain(level, FontB));
        }

        // ==================== Localized text ====================

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Build_LocalizedText_ProducesOneEntryPerLanguage()
        {
            var level = CreateLevel();
            AddLocalizedText(level, FontA, new StringLanguage("en", "ab"), new StringLanguage("ru", "яб"));

            var cached = FontCharacterService.Build(level.Game, FontA);

            Assert.IsInstanceOf<StringLocalized>(cached.Characters);
            Assert.AreEqual("ab", LanguageOf(cached, "en"));
            Assert.AreEqual("бя", LanguageOf(cached, "ru"));
        }

        // A non-localized text renders whatever language the player picked, so its glyphs are needed
        // in every language's set - not in one of them, and not in a set of their own.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Build_PlainTextAlongsideLocalized_LandsInEveryLanguage()
        {
            var level = CreateLevel();
            AddText(level, FontA, "0");
            AddLocalizedText(level, FontA, new StringLanguage("en", "a"), new StringLanguage("ru", "я"));

            var cached = FontCharacterService.Build(level.Game, FontA);

            Assert.AreEqual("0a", LanguageOf(cached, "en"));
            Assert.AreEqual("0я", LanguageOf(cached, "ru"));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Build_LocalizedEntryWithoutLanguageCode_FallsBackToDefaultCode()
        {
            var level = CreateLevel();
            AddLocalizedText(level, FontA, new StringLanguage(string.Empty, "ab"));

            var cached = FontCharacterService.Build(level.Game, FontA);

            Assert.AreEqual("ab", LanguageOf(cached, ValueRules.DefaultLanguageCode));
        }

        // ==================== Appearing mask ====================

        // Every TextObject carries a mask whether or not it ever animates, so the default one is
        // always in the set. One glyph per font, and the alternative - warming it only when an
        // Appearing track exists - would break the moment an author adds that track mid-session.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Build_DefaultMaskIsIncluded()
        {
            var level = CreateLevel();
            var obj = AddText(level, FontA, "ab");
            obj.AppearingMask = TextRules.AppearingMask_Default;

            Assert.AreEqual("Xab", BuildPlain(level, FontA));
        }

        // A mask character is drawn in place of the text, so it needs a glyph as much as the text
        // does - and missing it only shows up the moment the effect runs, as boxes.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Build_IncludesTheAppearingMask()
        {
            var level = CreateLevel();
            AddText(level, FontA, "ab").AppearingMask = "#@";

            Assert.AreEqual("#@ab", BuildPlain(level, FontA));
        }

        // The mask replaces text in whatever language is being read, so it belongs to every language's
        // set rather than to one of them.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Build_MaskLandsInEveryLanguage()
        {
            var level = CreateLevel();
            AddLocalizedText(level, FontA, new StringLanguage("en", "a"), new StringLanguage("ru", "я"))
                .AppearingMask = "#";

            var cached = FontCharacterService.Build(level.Game, FontA);

            Assert.AreEqual("#a", LanguageOf(cached, "en"));
            Assert.AreEqual("#я", LanguageOf(cached, "ru"));
        }

        // ==================== Determinism and bounds ====================

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Build_SameContentDifferentObjectOrder_ProducesIdenticalString()
        {
            var first = CreateLevel();
            AddText(first, FontA, "zebra");
            AddText(first, FontA, "quick");

            var second = CreateLevel();
            AddText(second, FontA, "quick");
            AddText(second, FontA, "zebra");

            Assert.AreEqual(BuildPlain(first, FontA), BuildPlain(second, FontA));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Build_SortedAscending()
        {
            var level = CreateLevel();
            AddText(level, FontA, "dcba");

            var built = BuildPlain(level, FontA);

            CollectionAssert.AreEqual(built.OrderBy(c => c).ToArray(), built.ToArray());
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Hard)]
        public void Build_MoreDistinctCharactersThanTheCap_TruncatesInsteadOfFailing()
        {
            var level = CreateLevel();
            var many = new string(Enumerable.Range(0x4E00, TextRules.MaxFontBufferSize + 200)
                .Select(c => (char)c).ToArray());
            AddText(level, FontA, many);

            Assert.AreEqual(TextRules.MaxFontBufferSize, BuildPlain(level, FontA).Length);
        }

        // ==================== BuildAll / Apply ====================

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void BuildAll_CoversGameDefinedAndUserDefinedFontsAlike()
        {
            var level = CreateLevel();
            AddText(level, FontA, "abc");
            AddText(level, FontB, "xyz");

            var built = FontCharacterService.BuildAll(level.Game);

            Assert.AreEqual(2, built.Count);
            Assert.IsTrue(built.ContainsKey(FontA));
            Assert.IsTrue(built.ContainsKey(FontB));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void BuildAll_EveryValueCarriesItsOwnKey()
        {
            var level = CreateLevel();
            AddText(level, FontA, "abc");
            AddText(level, FontB, "xyz");

            foreach (var (fontResourceId, cached) in FontCharacterService.BuildAll(level.Game))
                Assert.AreEqual(fontResourceId, cached.FontResourceId);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void BuildAll_NoTextObjects_ProducesNothing()
        {
            var level = CreateLevel();

            Assert.IsEmpty(FontCharacterService.BuildAll(level.Game));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Apply_RemoveUnused_DropsEntriesNoTextReferencesAnyMore()
        {
            var level = CreateLevel();
            AddText(level, FontA, "abc");
            level.Hints.FontCharacters[FontB] = new CachedFontText(FontB, new StringValue("stale"));

            FontCharacterService.Apply(level.Hints, level.Game);

            Assert.IsTrue(level.Hints.FontCharacters.ContainsKey(FontA));
            Assert.IsFalse(level.Hints.FontCharacters.ContainsKey(FontB));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Apply_KeepUnused_LeavesStaleEntriesAlone()
        {
            var level = CreateLevel();
            AddText(level, FontA, "abc");
            level.Hints.FontCharacters[FontB] = new CachedFontText(FontB, new StringValue("stale"));

            FontCharacterService.Apply(level.Hints, level.Game, removeUnused: false);

            Assert.AreEqual("stale", PlainOf(level.Hints.FontCharacters[FontB]));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void CollectFontIds_ReturnsEveryReferencedFontOnce()
        {
            var level = CreateLevel();
            AddText(level, FontA, "a");
            AddText(level, FontA, "b");
            AddText(level, FontB, "c");

            var ids = FontCharacterService.CollectFontIds(level.Game);

            Assert.AreEqual(2, ids.Count);
        }

        // ==================== Astral characters ====================

        // This set is WRITTEN INTO level.json, so a surrogate half in it is a broken character in the
        // file rather than a rendering artifact. An astral character reaches here by both doors - a
        // text object's own string, and an appearing mask, which may legitimately carry one since
        // TextComposeJob picks whole characters out of it.

        /// <summary> U+1F44D THUMBS UP - one character, two code units, escaped so the fixture never
        /// depends on this file's own encoding. </summary>
        private const string Thumb = "👍";

        /// <summary> Fails on any surrogate not sitting beside its own other half. </summary>
        private static void AssertNoLoneSurrogate(string value)
        {
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (char.IsHighSurrogate(character))
                {
                    Assert.IsTrue(index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]),
                        $"A high surrogate at {index} of the set has no low half");
                    index++;
                    continue;
                }

                Assert.IsFalse(char.IsLowSurrogate(character),
                    $"A low surrogate at {index} of the set has no high half");
            }
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Build_TextCarryingAnAstralCharacter_KeepsItWhole()
        {
            var level = CreateLevel();
            AddText(level, FontA, "a" + Thumb + "b");

            var set = BuildPlain(level, FontA);

            AssertNoLoneSurrogate(set);
            StringAssert.Contains(Thumb, set);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Build_MaskCarryingAnAstralCharacter_KeepsItWhole()
        {
            var level = CreateLevel();
            var text = AddText(level, FontA, "ab");
            text.AppearingMask = Thumb;

            var set = BuildPlain(level, FontA);

            AssertNoLoneSurrogate(set);
            StringAssert.Contains(Thumb, set);
        }

        // Sorting moved from code unit to code point with the fix, and a set is only worth trusting
        // if it is reproducible: the same characters in any authored order produce the same string.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Build_AstralCharacters_AreOrderedReproducibly()
        {
            var first = CreateLevel();
            AddText(first, FontA, Thumb + "a");

            var second = CreateLevel();
            AddText(second, FontA, "a" + Thumb);

            Assert.AreEqual(BuildPlain(first, FontA), BuildPlain(second, FontA));
        }
    }
}
