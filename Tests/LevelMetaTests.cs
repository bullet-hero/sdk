using System.Collections.Generic;
using BH.SDK.Models;
using Newtonsoft.Json;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // The two fields the level BROWSER needs and that nothing else on this model could supply.
    //
    // Both ride object initializers in Copy() rather than further constructor parameters - the same
    // call LevelSettings.Seed made - and an initializer is the one shape a copy-paste silently drops.
    // ModelContractTests cannot catch that: it sweeps REFERENCES on default-constructed pairs, so a
    // Copy that loses a value field, or one that shares a list instance instead of copying it, both
    // pass it.
    //
    // LevelDuration's zero is UNKNOWN, not "instant". Every level written before the field existed
    // reads back that way, which is exactly why the browser's duration filter may never hide it.

    public class LevelMetaTests
    {
        private static LevelMeta Authored()
        {
            var meta = new LevelMeta();
            meta.LevelTags = new List<string> { "boss", "hard" };
            meta.LevelDuration = 137.5f;
            return meta;
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Defaults_AreNoTagsAndUnknownLength()
        {
            var meta = new LevelMeta();

            Assert.IsNotNull(meta.LevelTags);
            Assert.IsEmpty(meta.LevelTags);
            Assert.AreEqual(0f, meta.LevelDuration);
        }

        // The reason this file exists. Both fields, and the list checked for being a COPY rather than
        // the same instance - a shared list would let an edit to one level's tags reach another.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Copy_CarriesTheInitializerFields()
        {
            var source = Authored();

            var copy = source.Copy();

            Assert.AreEqual(137.5f, copy.LevelDuration);
            CollectionAssert.AreEqual(source.LevelTags, copy.LevelTags);
            Assert.AreNotSame(source.LevelTags, copy.LevelTags);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void UpdateAndPull_CarryTheInitializerFields()
        {
            var source = Authored();

            var updated = new LevelMeta();
            updated.Update(source);
            var pulled = new LevelMeta();
            pulled.Pull(source);

            CollectionAssert.AreEqual(source.LevelTags, updated.LevelTags);
            CollectionAssert.AreEqual(source.LevelTags, pulled.LevelTags);
            Assert.AreEqual(137.5f, updated.LevelDuration);
            Assert.AreEqual(137.5f, pulled.LevelDuration);

            Assert.AreNotSame(source.LevelTags, updated.LevelTags);
            Assert.AreNotSame(source.LevelTags, pulled.LevelTags);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Reset_ClearsBoth()
        {
            var meta = Authored();

            meta.Reset();

            Assert.IsEmpty(meta.LevelTags);
            Assert.AreEqual(0f, meta.LevelDuration);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void RoundTrip_KeepsBoth()
        {
            var source = Authored();

            var json = JsonConvert.SerializeObject(source);
            var restored = JsonConvert.DeserializeObject<LevelMeta>(json);

            CollectionAssert.AreEqual(source.LevelTags, restored.LevelTags);
            Assert.AreEqual(source.LevelDuration, restored.LevelDuration);
        }

        // The claim that made both fields additive: no DataVersion bump, no migrator. An absent key
        // is never written, so the constructor's empty list and zero survive - and zero is exactly
        // the "unknown" the browser must not filter on.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void MetadataWrittenBeforeTheFields_ReadsBackEmptyAndUnknown()
        {
            var meta = JsonConvert.DeserializeObject<LevelMeta>("{\"age\":0}");

            Assert.IsNotNull(meta.LevelTags);
            Assert.IsEmpty(meta.LevelTags);
            Assert.AreEqual(0f, meta.LevelDuration);
        }
    }
}
