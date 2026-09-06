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
    // GeneratorSweepTests already pins Estimate against Objects and Keyframes for every registered
    // generator, but it does not look at Resources - and Resources is the only thing this generator
    // reports. A host refuses a Content run whose estimate is entirely zero, so an estimate that
    // drifted here would not show up as a wrong number, it would show up as the generator being
    // permanently greyed out.
    public class FontCacheGeneratorEstimateTests
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
            };
            level.Game.Objects.Add(obj.ObjectId, obj);
        }

        private static GeneratorCost Estimate(Level level, bool removeUnused = true)
        {
            var generator = new FontCacheGenerator();
            var context = new GeneratorContext(level, FrameSpan.FromBounds(0, FrameDuration));
            return generator.Estimate(context, new FontCacheGenerator.Parameters { RemoveUnused = removeUnused });
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Estimate_CountsOneResourcePerFontInUse()
        {
            var level = CreateLevel();
            AddText(level, FontA, "abc");
            AddText(level, FontB, "xyz");

            Assert.AreEqual(2, Estimate(level).Resources);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Estimate_CountsStaleEntriesItWouldRemove()
        {
            var level = CreateLevel();
            AddText(level, FontA, "abc");
            level.Hints.FontCharacters[FontB] = new CachedFontText(FontB, new StringValue("stale"));

            Assert.AreEqual(2, Estimate(level).Resources);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Estimate_RemoveUnusedOff_IgnoresStaleEntries()
        {
            var level = CreateLevel();
            AddText(level, FontA, "abc");
            level.Hints.FontCharacters[FontB] = new CachedFontText(FontB, new StringValue("stale"));

            Assert.AreEqual(1, Estimate(level, removeUnused: false).Resources);
        }

        // Correctly zero, and therefore correctly refused by a host: there is genuinely nothing to
        // build or drop.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Estimate_NoTextAndNoStaleEntries_IsZero()
        {
            Assert.AreEqual(GeneratorCost.Zero, Estimate(CreateLevel()));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Estimate_CreatesNoObjectsOrKeyframes()
        {
            var level = CreateLevel();
            AddText(level, FontA, "abc");

            var cost = Estimate(level);

            Assert.AreEqual(0, cost.Objects);
            Assert.AreEqual(0, cost.Keyframes);
        }
    }
}
