using BH.SDK.Generators;
using BH.SDK.Generators.Utility;
using BH.SDK.Models;
using BH.SDK.Models.Data;
using BH.SDK.Models.Objects;
using BH.SDK.Models.Primitives;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Values;
using NUnit.Framework;

namespace BH.SDK.Tests.Generators
{
    // The property worth pinning here is undo, not the character sets - those are
    // FontCharacterServiceTests' job. FontCharacters is a resource dictionary, so it has a journal
    // entry shape and a run of this generator has no excuse to be irreversible. Writing to the
    // dictionary directly compiles and runs identically right up until someone presses Undo, which
    // is exactly the failure the reverting tests below exist to catch.

    /// <summary> Undo, not the character sets - writing to the resource dictionary directly runs identically
    /// right up until someone presses Undo. </summary>
    public class FontCacheGeneratorTests
    {
        private const int FrameDuration = 600;

        private static readonly FontResourceId FontA = new(1);
        private static readonly FontResourceId FontB = new(-1);

        private static Level CreateLevel()
        {
            var level = new Level();
            level.Settings.Fps = 60;
            level.Settings.FrameDuration = FrameDuration;
            return level;
        }

        private static void AddText(Level level, FontResourceId fontResourceId, string text)
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
        }

        private static GeneratorContext Context(Level level)
            => new(level, FrameSpan.FromBounds(0, FrameDuration));

        private static GeneratorResult Run(Level level, GeneratorContext context, bool removeUnused = true)
        {
            var generator = new FontCacheGenerator();
            var parameters = new FontCacheGenerator.Parameters { RemoveUnused = removeUnused };
            return generator.Run(context, parameters);
        }

        private static string ValueOf(Level level, FontResourceId fontResourceId)
            => ((StringValue)level.Hints.FontCharacters[fontResourceId].Characters).Value;

        // ==================== Building ====================

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Run_EmptyCache_FillsItFromTheLevelsText()
        {
            var level = CreateLevel();
            AddText(level, FontA, "cab");

            Run(level, Context(level));

            Assert.AreEqual("abc", ValueOf(level, FontA));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Run_Twice_IsIdempotent()
        {
            var level = CreateLevel();
            AddText(level, FontA, "abc");

            Run(level, Context(level));
            var first = ValueOf(level, FontA);

            Run(level, Context(level));

            Assert.AreEqual(first, ValueOf(level, FontA));
            Assert.AreEqual(1, level.Hints.FontCharacters.Count);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Run_StaleEntry_IsRemoved()
        {
            var level = CreateLevel();
            AddText(level, FontA, "abc");
            level.Hints.FontCharacters[FontB] = new CachedFontText(FontB, new StringValue("stale"));

            Run(level, Context(level));

            Assert.IsFalse(level.Hints.FontCharacters.ContainsKey(FontB));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Run_RemoveUnusedOff_KeepsStaleEntry()
        {
            var level = CreateLevel();
            AddText(level, FontA, "abc");
            level.Hints.FontCharacters[FontB] = new CachedFontText(FontB, new StringValue("stale"));

            Run(level, Context(level), removeUnused: false);

            Assert.AreEqual("stale", ValueOf(level, FontB));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Run_CreatesNoObjects()
        {
            var level = CreateLevel();
            AddText(level, FontA, "abc");

            var result = Run(level, Context(level));

            Assert.IsEmpty(result.CreatedIds);
        }

        // ==================== Undo (the reason every write goes through the context) ====================

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Revert_FromEmptyCache_LeavesItEmptyAgain()
        {
            var level = CreateLevel();
            AddText(level, FontA, "abc");

            var context = Context(level);
            Run(level, context);
            context.Log.Revert();

            Assert.IsEmpty(level.Hints.FontCharacters);
        }

        // The one an in-place overwrite gets wrong: ResourceAdded.Revert removes the key it added, so
        // replacing an entry without journaling the removal first deletes what was there before.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Revert_OverExistingEntry_RestoresThePreviousValue()
        {
            var level = CreateLevel();
            AddText(level, FontA, "abc");
            level.Hints.FontCharacters[FontA] = new CachedFontText(FontA, new StringValue("previous"));

            var context = Context(level);
            Run(level, context);
            context.Log.Revert();

            Assert.AreEqual("previous", ValueOf(level, FontA));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Revert_AfterRemovingStaleEntry_PutsItBack()
        {
            var level = CreateLevel();
            AddText(level, FontA, "abc");
            level.Hints.FontCharacters[FontB] = new CachedFontText(FontB, new StringValue("stale"));

            var context = Context(level);
            Run(level, context);
            context.Log.Revert();

            Assert.AreEqual("stale", ValueOf(level, FontB));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void RevertThenReapply_LandsBackOnTheGeneratedResult()
        {
            var level = CreateLevel();
            AddText(level, FontA, "abc");

            var context = Context(level);
            Run(level, context);
            var generated = ValueOf(level, FontA);

            context.Log.Revert();
            context.Log.Reapply();

            Assert.AreEqual(generated, ValueOf(level, FontA));
        }
    }
}
