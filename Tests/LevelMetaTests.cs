using System.Collections.Generic;
using System.Linq;
using BH.SDK.Models;
using BH.SDK.Serialization;
using BH.SDK.Serialization.Serializers;
using BH.SDK.Versions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace BH.SDK.Tests
{
    // The three fields the level BROWSER needs and that nothing else on this model could supply.
    //
    // All three ride object initializers in Copy() rather than further constructor parameters - the
    // same call LevelSettings.Seed made - and an initializer is the one shape a copy-paste silently
    // drops. ModelContractTests cannot catch that: it sweeps REFERENCES on default-constructed
    // pairs, so a Copy that loses a value field, or one that shares a list instance instead of
    // copying it, both pass it.
    //
    // LevelDuration's zero is UNKNOWN, not "instant". Every level written before the field existed
    // reads back that way, which is exactly why the browser's duration filter may never hide it.
    //
    // MinGeneration says the same kind of thing about a different question - what a client needs to
    // read this level whole - and answers it WITHOUT opening the level, which is the whole reason it
    // lives in metadata. Its "unknown" cannot be zero, because zero is a real generation.


    /// <summary> The three fields the level browser needs, all riding object initializers in Copy - the one
    /// shape a copy-paste drops silently and the contract sweep cannot see. </summary>
    public class LevelMetaTests
    {
        private static LevelMeta Authored()
        {
            var meta = new LevelMeta();
            meta.LevelTags = new List<string> { "boss", "hard" };
            meta.LevelDuration = 137.5f;
            meta.MinGeneration = LevelGenerations.Required();
            return meta;
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Defaults_AreNoTagsUnknownLengthAndNoClaim()
        {
            var meta = new LevelMeta();

            Assert.IsNotNull(meta.LevelTags);
            Assert.IsEmpty(meta.LevelTags);
            Assert.AreEqual(0f, meta.LevelDuration);
            Assert.AreEqual(ModelGenerations.Invalid, meta.MinGeneration);
        }

        // The reason this file exists. All three fields, and the list checked for being a COPY rather
        // than the same instance - a shared list would let an edit to one level's tags reach another.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Copy_CarriesTheInitializerFields()
        {
            var source = Authored();

            var copy = source.Copy();

            Assert.AreEqual(137.5f, copy.LevelDuration);
            Assert.AreEqual(source.MinGeneration, copy.MinGeneration);
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
            Assert.AreEqual(source.MinGeneration, updated.MinGeneration);
            Assert.AreEqual(source.MinGeneration, pulled.MinGeneration);

            Assert.AreNotSame(source.LevelTags, updated.LevelTags);
            Assert.AreNotSame(source.LevelTags, pulled.LevelTags);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Reset_ClearsAllThree()
        {
            var meta = Authored();

            meta.Reset();

            Assert.IsEmpty(meta.LevelTags);
            Assert.AreEqual(0f, meta.LevelDuration);
            Assert.AreEqual(ModelGenerations.Invalid, meta.MinGeneration);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void RoundTrip_KeepsAllThree()
        {
            var source = Authored();

            var json = JsonConvert.SerializeObject(source);
            var restored = JsonConvert.DeserializeObject<LevelMeta>(json);

            CollectionAssert.AreEqual(source.LevelTags, restored.LevelTags);
            Assert.AreEqual(source.LevelDuration, restored.LevelDuration);
            Assert.AreEqual(source.MinGeneration, restored.MinGeneration);
        }

        // The claim that made all three fields additive: no generation bump, no migrator. An absent
        // key is never written, so the constructor's empty list, zero and Invalid survive - and each
        // of those is exactly the "unknown" its own consumer must not act on.
        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        public void MetadataWrittenBeforeTheFields_ReadsBackEmptyUnknownAndUnclaimed()
        {
            var meta = JsonConvert.DeserializeObject<LevelMeta>("{\"age\":0}");

            Assert.IsNotNull(meta.LevelTags);
            Assert.IsEmpty(meta.LevelTags);
            Assert.AreEqual(0f, meta.LevelDuration);
            Assert.AreEqual(ModelGenerations.Invalid, meta.MinGeneration);
        }

        #region The claim itself

        // WHAT A CLIENT MUST SUPPORT TO READ THIS LEVEL, computed rather than typed. It has to be
        // computed: a number an author could write is a number that goes stale the first time a
        // domain moves, and the whole value of this key is that an older build can trust it.

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void Required_IsTheHighestGenerationALevelsOwnDomainsAreAt()
        {
            var highest = LevelGenerations.Domains
                .Max(d => VersionedTypeRegistry.GetLatestAttribute(d).Generation);

            Assert.AreEqual(highest, LevelGenerations.Required());
            Assert.Greater(LevelGenerations.Required(), ModelGenerations.Invalid);
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.VeryEasy)]
        public void EveryDomainALevelHolds_IsARegisteredDomain()
        {
            // The list is hand-kept - nothing in a domain NAME says whether a level file can contain
            // it - so what is checked is the half a typo would break: that each one exists at all.
            foreach (var domain in LevelGenerations.Domains)
                Assert.DoesNotThrow(() => VersionedTypeRegistry.GetLatestAttribute(domain), domain);

            CollectionAssert.AllItemsAreUnique(LevelGenerations.Domains);
            CollectionAssert.DoesNotContain(LevelGenerations.Domains, ModelDomains.LevelMeta,
                "metadata is the file the claim is written INTO, not a domain it covers");
        }

        [Test]
        [Author(Metadata.Author.Vertoker)]
        [Category(Metadata.Category.Self)]
        [Category(Metadata.Category.Easy)]
        [TestCase(SerializationType.Json)]
        [TestCase(SerializationType.Blob)]
        public void TheClaim_SurvivesARoundTripThroughEitherFormat(SerializationType format)
        {
            var service = new SerializationService();
            var serializer = service.GetDataSerializer(format);

            var meta = Authored();

            var bytes = serializer.SerializeEnvelope(ModelDomains.LevelMeta,
                new EnvelopeData(ModelGenerations.Release, meta));
            var read = serializer.DeserializeEnvelope(bytes, typeof(LevelMeta)).GetPayload<LevelMeta>();

            Assert.AreEqual(meta.MinGeneration, read.MinGeneration);
        }

        #endregion
    }
}
