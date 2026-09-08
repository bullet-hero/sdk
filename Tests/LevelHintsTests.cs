using BH.SDK.Models;
using BH.SDK.Models.Data;
using BH.SDK.Models.Hints;
using BH.SDK.Models.Primitives.Resources;
using BH.SDK.Models.Values;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // The aggregate's own contract, separate from what its two members do: a hint is optional data,
    // so "a fresh level has an empty one" and "an empty one is what a level without hints reads as"
    // are the two facts everything downstream leans on.

    /// <summary> The hints aggregate's own contract - that a fresh level carries an empty one, and that a level
    /// without hints reads back as exactly that. </summary>
    [TestFixture]
    public class LevelHintsTests
    {
        private static CachedFontText Cached(int id, string characters) =>
            new(new FontResourceId(id), new StringValue(characters));

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void NewLevel_HasEmptyHints()
        {
            var level = new Level();

            Assert.IsNotNull(level.Hints);
            Assert.IsNotNull(level.Hints.Limits);
            Assert.IsNotNull(level.Hints.FontCharacters);
            Assert.IsFalse(level.Hints.HasValue, "nothing has been measured yet");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void HasValue_IsTrue_WhenEitherMemberCarriesSomething()
        {
            var limitsOnly = new LevelHints { Limits = new LimitHints(4, 1, 3, 0, 0, 0) };
            Assert.IsTrue(limitsOnly.HasValue);

            var fontsOnly = new LevelHints();
            fontsOnly.FontCharacters.Add(new FontResourceId(1), Cached(1, "abc"));
            Assert.IsTrue(fontsOnly.HasValue);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Copy_IsDeep_AndEqual()
        {
            var source = new LevelHints { Limits = new LimitHints(9, 2, 7, 1, 3, 2) };
            source.FontCharacters.Add(new FontResourceId(-1), Cached(-1, " ABC"));

            var copy = source.Copy();

            Assert.IsTrue(source.Equals(copy));
            Assert.AreEqual(source, copy, "Equals(object) must resolve to the typed overload");

            copy.Limits.Instances = 100;
            copy.FontCharacters.Clear();

            Assert.AreEqual(9, source.Limits.Instances, "the copy must not share its Limits instance");
            Assert.AreEqual(1, source.FontCharacters.Count, "nor its dictionary");
            Assert.IsFalse(source.Equals(copy));
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void Reset_ClearsBothMembers_InPlace()
        {
            var hints = new LevelHints { Limits = new LimitHints(9, 2, 7, 1, 3, 2) };
            hints.FontCharacters.Add(new FontResourceId(1), Cached(1, "abc"));
            var limits = hints.Limits;

            hints.Reset();

            Assert.IsFalse(hints.HasValue);
            Assert.AreSame(limits, hints.Limits, "Reset is in place, it does not reallocate");
            Assert.IsEmpty(hints.FontCharacters);
        }

        // A level written before Hints existed carries no "hints" key at all, and must come back as
        // an empty aggregate rather than null - the same no-migration shape LevelSettings.Seed and
        // GameEvents.Beats were added in. This is what makes the whole move free of a migration.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Level_WithoutHintsKey_DeserializesToEmptyHints()
        {
            var service = new SerializationService(new SerializationSettings());

            var json = service.SerializeData(new Level());
            var stripped = json.Replace($"\"{Names.Hints}\":", "\"unused_hints\":");
            var restored = service.DeserializeData<Level>(stripped);

            Assert.IsNotNull(restored.Hints);
            Assert.IsFalse(restored.Hints.HasValue);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Normal)]
        public void Level_RoundTrips_ItsHints()
        {
            var service = new SerializationService(new SerializationSettings());

            var level = new Level();
            level.Hints.Limits = new LimitHints(12, 5, 7, 2, 1, 3);
            level.Hints.FontCharacters.Add(new FontResourceId(1), Cached(1, " ABCabc"));

            var restored = service.DeserializeData<Level>(service.SerializeData(level));

            Assert.IsTrue(level.Hints.Equals(restored.Hints));
        }
    }
}
